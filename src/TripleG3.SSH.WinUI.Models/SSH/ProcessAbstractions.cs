using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.SSH;

public interface IProcessWrapper : IDisposable
{
    event Action? Exited;
    StreamReader StandardOutput { get; }
    StreamReader StandardError { get; }
    StreamWriter StandardInput { get; }
    bool HasExited { get; }
    int ExitCode { get; }
    void Start();
    Task WaitForExitAsync(CancellationToken cancellationToken = default);
    void WaitForExit(int milliseconds);
    void Kill(bool entireProcessTree);
}

public interface IProcessFactory
{
    IProcessWrapper Create(ProcessStartInfo startInfo, bool enableRaisingEvents = true);
}

internal sealed class SystemProcessWrapper : IProcessWrapper
{
    private readonly Process _proc;

    public event Action? Exited;

    public SystemProcessWrapper(Process proc)
    {
        _proc = proc;
        _proc.EnableRaisingEvents = true;
        _proc.Exited += (_, _) => Exited?.Invoke();
    }

    public StreamReader StandardOutput => _proc.StandardOutput;
    public StreamReader StandardError => _proc.StandardError;
    public StreamWriter StandardInput => _proc.StandardInput;
    public bool HasExited => _proc.HasExited;
    public int ExitCode => _proc.ExitCode;

    public void Start()
    {
        if (!_proc.Start())
            throw new InvalidOperationException("Failed to start the process.");
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) => _proc.WaitForExitAsync(cancellationToken);

    public void WaitForExit(int milliseconds) => _proc.WaitForExit(milliseconds);

    public void Kill(bool entireProcessTree) => _proc.Kill(entireProcessTree);

    public void Dispose() => _proc.Dispose();
}

public sealed class SystemProcessFactory : IProcessFactory
{
    public IProcessWrapper Create(ProcessStartInfo startInfo, bool enableRaisingEvents = true)
    {
        var p = new Process { StartInfo = startInfo, EnableRaisingEvents = enableRaisingEvents };
        return new SystemProcessWrapper(p);
    }
}
