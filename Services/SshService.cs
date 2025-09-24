using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using SSHExplorer.Models;

namespace SSHExplorer.Services;

public sealed class SshService : StatePublisher<SshConnectionState>, ISshService
{
    private SshClient? _ssh;
    private SftpClient? _sftp;

    public SshService() : base(SshConnectionState.Empty)
    {
    }

    public async Task ConnectAsync(Profile profile, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });
        
        try
        {
            await Task.Run(() =>
            {
                ConnectionInfo connInfo;
                if (profile.UseKeyAuth && !string.IsNullOrWhiteSpace(profile.PrivateKeyPath))
                {
                    var keyFile = string.IsNullOrWhiteSpace(profile.Passphrase)
                        ? new PrivateKeyFile(profile.PrivateKeyPath)
                        : new PrivateKeyFile(profile.PrivateKeyPath, profile.Passphrase);
                    var auth = new PrivateKeyAuthenticationMethod(profile.Username, keyFile);
                    connInfo = new ConnectionInfo(profile.Host, profile.Port, profile.Username, auth);
                }
                else
                {
                    // Use empty string if password is null to avoid ctor exceptions; server will still enforce auth
                    connInfo = new PasswordConnectionInfo(profile.Host, profile.Port, profile.Username, profile.Password ?? string.Empty);
                }

                _ssh = new SshClient(connInfo);
                _sftp = new SftpClient(connInfo);
                _ssh.Connect();
                _sftp.Connect();
            }, ct);

            SetState(State with 
            { 
                IsBusy = false, 
                IsConnected = true, 
                RemotePath = profile.DefaultRemotePath,
                LocalPath = profile.DefaultLocalPath,
                ConnectedProfile = profile,
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, IsConnected = false, ErrorMessage = ex.Message });
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        SetState(State with { IsBusy = true });
        
        try
        {
            await Task.Run(() =>
            {
                try { if (_ssh?.IsConnected == true) _ssh.Disconnect(); } catch { }
                try { if (_sftp?.IsConnected == true) _sftp.Disconnect(); } catch { }
                _ssh?.Dispose();
                _sftp?.Dispose();
                _ssh = null;
                _sftp = null;
            });

            SetState(State with 
            { 
                IsBusy = false, 
                IsConnected = false, 
                ConnectedProfile = null, 
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task<IEnumerable<SftpFile>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        try
        {
            return await Task.Run(() =>
            {
                var results = _sftp.ListDirectory(path);
                // Materialize to avoid deferred enumeration exceptions outside callers' try/catch
                return results.ToList();
            }, ct);
        }
        catch (SftpPermissionDeniedException ex)
        {
            var friendlyMessage = $"Access denied to folder '{path}'. You don't have permission to view this directory.";
            SetState(State with { ErrorMessage = friendlyMessage });
            throw new UnauthorizedAccessException(friendlyMessage, ex);
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Failed to list '{path}': {ex.Message}" });
            throw new IOException($"Failed to list '{path}': {ex.Message}", ex);
        }
    }

    public async Task DownloadFileAsync(string remotePath, string localPath, IProgress<ulong>? progress = null, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        
        try
        {
            await Task.Run(() =>
            {
                using var fs = File.OpenWrite(localPath);
                _sftp.DownloadFile(remotePath, fs, uploaded => progress?.Report(uploaded));
            }, ct);
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Download failed: {ex.Message}" });
            throw;
        }
    }

    public async Task UploadFileAsync(string localPath, string remotePath, IProgress<ulong>? progress = null, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        
        try
        {
            await Task.Run(() =>
            {
                using var fs = File.OpenRead(localPath);
                _sftp.UploadFile(fs, remotePath, uploaded => progress?.Report(uploaded));
            }, ct);
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Upload failed: {ex.Message}" });
            throw;
        }
    }

    public async Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default)
    {
        if (_ssh is null) throw new InvalidOperationException("Not connected");
        
        try
        {
            return await Task.Run(() =>
            {
                using var cmd = _ssh.CreateCommand(command);
                var result = cmd.Execute();
                return string.IsNullOrWhiteSpace(result) ? cmd.Error : result;
            }, ct);
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Command execution failed: {ex.Message}" });
            throw;
        }
    }

    public async Task ChangeDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        
        try
        {
            // Test that we can actually list the directory, not just that it exists
            await Task.Run(() =>
            {
                // This will throw SftpPermissionDeniedException if we can't access the directory
                var _ = _sftp.ListDirectory(path).Take(1).ToList();
            }, ct);
            
            SetState(State with { RemotePath = path });
        }
        catch (SftpPermissionDeniedException ex)
        {
            var friendlyMessage = $"Access denied to folder '{path}'. You don't have permission to enter this directory.";
            SetState(State with { ErrorMessage = friendlyMessage });
            throw new UnauthorizedAccessException(friendlyMessage, ex);
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Failed to change to '{path}': {ex.Message}" });
            throw new IOException($"Failed to open '{path}': {ex.Message}", ex);
        }
    }

    public ValueTask DisposeAsync()
    {
        try { _ssh?.Disconnect(); } catch { }
        try { _sftp?.Disconnect(); } catch { }
        _ssh?.Dispose();
        _sftp?.Dispose();
        return ValueTask.CompletedTask;
    }
}
