using Specky7;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.SSH;

[Singleton<ISSHService>]
public class SSHService(ISshHistoryStore historyStore, IProcessFactory processFactory) : ISSHService
{
    public event Action<SSHState> StateChanged = delegate { };
    public event Action<string> OutputReceived = delegate { };
    public event Action<string> ErrorReceived = delegate { };

    // Per-command timeout. Set to TimeSpan.Zero to disable.
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(60);

    private SSHState state = SSHState.Empty;
    private IProcessWrapper? sshProcess;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    // History/session tracking
    private readonly object historyLock = new();
    private Guid currentSessionId = Guid.Empty;
    private DateTimeOffset sessionStartedAt;
    private readonly List<SshCommandExchange> exchanges = [];
    private readonly List<SshTimelineEntry> transcript = [];

    // Expose in-memory history for UI
    public IReadOnlyList<SshCommandExchange> Exchanges => exchanges;
    public IReadOnlyList<SshTimelineEntry> Transcript => transcript;

    public SSHState State
    {
        get => state;
        private set
        {
            if (value != state)
            {
                state = value;
                StateChanged(state);
            }
        }
    }

    public async ValueTask ConnectAsync(Profiles.Profile profile)
    {
        await semaphore.WaitAsync();
        if (State.IsBusy)
            throw new InvalidOperationException("SSHService is busy.");

        if (!string.IsNullOrWhiteSpace(profile.Password))
            throw new NotSupportedException("Password auth is not supported via the system ssh client. Configure key-based auth for the target host instead.");

        State = State with { IsBusy = true };

        if (State.IsConnected)
        {
            // Already connected to a host
            State = State with { IsBusy = false };
            semaphore.Release();
            throw new InvalidOperationException("Already connected to a host. Disconnect first.");
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ssh",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true, // allow sending commands
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            // Use -T (no TTY) so stdout/stderr remain separate for reliable parsing.
            psi.ArgumentList.Add("-T");
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add("BatchMode=yes");
            psi.ArgumentList.Add("-o");
            psi.ArgumentList.Add("StrictHostKeyChecking=accept-new");
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add(profile.Port.ToString());
            psi.ArgumentList.Add($"{profile.Username}@{profile.Address}");

            var proc = processFactory.Create(psi, enableRaisingEvents: true);
            proc.Exited += () =>
            {
                // When ssh exits (network drop, host closed, etc.), update state
                State = State with { IsConnected = false, IsBusy = false };
                proc.Dispose();
                if (sshProcess == proc) sshProcess = null;
            };

            proc.Start();

            // Give ssh a brief moment to fail fast if it cannot authenticate.
            await Task.Delay(600);

            if (proc.HasExited)
            {
                // Read any error output for diagnostics.
                string err = await proc.StandardError.ReadToEndAsync();
                throw new InvalidOperationException(
                    $"ssh exited with code {proc.ExitCode}. Error: {err}".Trim());
            }

            sshProcess = proc;
            State = new SSHState(profile, true, false);

            // Start a fresh session history
            lock (historyLock)
            {
                exchanges.Clear();
                transcript.Clear();
                currentSessionId = Guid.NewGuid();
                sessionStartedAt = DateTimeOffset.UtcNow;
                transcript.Add(new SshTimelineEntry(currentSessionId, sessionStartedAt, SshTimelineKind.Info, null, $"Connected to {profile.Username}@{profile.Address}:{profile.Port}"));
            }
        }
        catch
        {
            // Revert busy state on failure
            State = State with { IsBusy = false };
            // Ensure process is cleaned up if it was started
            if (sshProcess is { HasExited: false })
            {
                try { sshProcess.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }
            sshProcess?.Dispose();
            sshProcess = null;
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async ValueTask DisconnectAsync()
    {
        if (sshProcess == null)
            return;

        await semaphore.WaitAsync();

        try
        {
            // Persist the session history before tearing down
            SshSessionHistory? sessionToSave = null;
            var endedAt = DateTimeOffset.UtcNow;
            lock (historyLock)
            {
                if (currentSessionId != Guid.Empty && (exchanges.Count > 0 || transcript.Count > 0))
                {
                    transcript.Add(new SshTimelineEntry(currentSessionId, endedAt, SshTimelineKind.Info, null, "Disconnected"));
                    sessionToSave = new SshSessionHistory(
                        currentSessionId,
                        State.Profile,
                        sessionStartedAt,
                        endedAt,
                        [.. exchanges],
                        [.. transcript]);
                }
            }
            if (sessionToSave is not null)
            {
                try { await historyStore.SaveAsync(sessionToSave).ConfigureAwait(false); }
                catch { /* non-fatal for disconnect */ }
            }

            if (sshProcess is { HasExited: false })
            {
                try
                {
                    sshProcess.Kill(entireProcessTree: true);
                    sshProcess.WaitForExit(2000);
                }
                catch
                {
                    // Ignore kill/timeout errors
                }
            }

            sshProcess?.Dispose();
            sshProcess = null;

            if (State.IsConnected || State.IsBusy)
                State = State with { IsConnected = false, IsBusy = false };

            // Reset in-memory history for next session
            lock (historyLock)
            {
                exchanges.Clear();
                transcript.Clear();
                currentSessionId = Guid.Empty;
                sessionStartedAt = default;
            }

            return;
        }
        catch
        {
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async ValueTask SendAsync(string command)
    {
        await semaphore.WaitAsync();
        if (State.IsBusy)
            throw new InvalidOperationException("SSHService is busy.");
        if (!State.IsConnected || sshProcess is null || sshProcess.HasExited)
        {
            semaphore.Release();
            throw new InvalidOperationException("Not connected to any host.");
        }

        CancellationTokenSource? timeoutCts = null;
        var commandId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            State = State with { IsBusy = true };

            // Record "sent" in transcript
            lock (historyLock)
            {
                transcript.Add(new SshTimelineEntry(currentSessionId, startedAt, SshTimelineKind.Sent, commandId, command));
            }

            // Unique framing markers for this command
            var id = Guid.NewGuid().ToString("N");
            var startMarker = $"__SSH_START__:{id}";
            var endMarker = $"__SSH_END__:{id}";

            // Compose wrapped command:
            // - Print start marker
            // - Run user command
            // - Capture exit code
            // - Print end marker with exit code
            var wrapped =
                $"printf '%s\\n' '{startMarker}'; " +
                $"{command}; " +
                "rc=$?; " +
                $"printf '%s:%d\\n' '{endMarker}' \"$rc\"";

            await sshProcess.StandardInput.WriteLineAsync(wrapped);
            await sshProcess.StandardInput.FlushAsync();

            using var cts = new CancellationTokenSource();
            CommandExecutionResult result;
            if (CommandTimeout > TimeSpan.Zero)
            {
                timeoutCts = new CancellationTokenSource(CommandTimeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, timeoutCts.Token);
                result = await ReadUntilEndAsync(sshProcess, startMarker, endMarker, linked.Token, commandId).ConfigureAwait(false);
            }
            else
            {
                result = await ReadUntilEndAsync(sshProcess, startMarker, endMarker, cts.Token, commandId).ConfigureAwait(false);
            }

            var endedAt = DateTimeOffset.UtcNow;
            var exchange = new SshCommandExchange(commandId, startedAt, endedAt, command, result.ExitCode, result.StdOut, result.StdErr);
            lock (historyLock)
            {
                exchanges.Add(exchange);
            }

            if (exchange.ExitCode != 0)
            {
                throw new InvalidOperationException($"Command exited with code {exchange.ExitCode}. Error: {exchange.StdErr}".Trim());
            }
        }
        catch (OperationCanceledException) when (timeoutCts is not null && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"Command timed out after {CommandTimeout.TotalSeconds:0}s.");
        }
        finally
        {
            State = State with { IsBusy = false };
            semaphore.Release();
        }

        static async Task<CommandExecutionResult> ReadUntilEndAsync(IProcessWrapper proc, string start, string end, CancellationToken token, Guid cmdId)
        {
            var stdout = proc.StandardOutput;
            var stderr = proc.StandardError;

            var stdoutBuffer = new StringBuilder();
            var stderrBuffer = new StringBuilder();

            // Drain stderr concurrently (note: with -T, stderr is separate; with a TTY, it may be merged into stdout)
            var stderrTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var line = await stderr.ReadLineAsync(token).ConfigureAwait(false);
                        if (line is null) break;
                        // We cannot access instance members here, so raise no events in the static local. Callers will handle transcript and events.
                        stderrBuffer.AppendLine(line);
                    }
                }
                catch (OperationCanceledException) { /* expected on cancel */ }
            }, token);

            var sawStart = false;
            int? exit = null;

            try
            {
                while (true)
                {
                    var line = await stdout.ReadLineAsync(token).ConfigureAwait(false);
                    if (line is null)
                    {
                        throw new InvalidOperationException("SSH connection closed while waiting for command to complete.");
                    }

                    // Ignore everything until the start marker (banners, MOTD, previous command remnants)
                    if (!sawStart)
                    {
                        if (string.Equals(line, start, StringComparison.Ordinal))
                            sawStart = true;
                        continue;
                    }

                    if (line.StartsWith(end + ":", StringComparison.Ordinal))
                    {
                        var idx = line.LastIndexOf(':');
                        if (idx >= 0 && int.TryParse(line.AsSpan(idx + 1), out var code))
                            exit = code;
                        break;
                    }

                    stdoutBuffer.AppendLine(line);
                }
            }
            finally
            {
                try { await stderrTask.ConfigureAwait(false); } catch { /* ignore */ }
            }

            return new CommandExecutionResult(exit.GetValueOrDefault(0), stdoutBuffer.ToString(), stderrBuffer.ToString());
        }
    }

    public ValueTask SendAsync(SshCommand command)
    {
        // Reuse the existing string-based pipeline and framing
        return SendAsync(command.Build());
    }

    // Optional: Load persisted history for the current profile (previous sessions)
    public Task<IReadOnlyList<SshSessionHistory>> LoadPersistedHistoryAsync() => historyStore.LoadAsync(State.Profile);

    // Result tuple for internal command execution
    private readonly record struct CommandExecutionResult(int ExitCode, string StdOut, string StdErr);
}
