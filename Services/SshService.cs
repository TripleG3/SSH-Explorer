using Renci.SshNet;
using Renci.SshNet.Sftp;
using SSHExplorer.Models;

namespace SSHExplorer.Services;

public sealed class SshService : ISshService
{
    private SshClient? _ssh;
    private SftpClient? _sftp;
    private string _currentDir = "/";

    public bool IsConnected => _ssh?.IsConnected == true && _sftp?.IsConnected == true;
    public string CurrentDirectory => _currentDir;

    public async Task ConnectAsync(Profile profile, CancellationToken ct = default)
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
            _currentDir = profile.DefaultRemotePath;
        }, ct);
    }

    public async Task<IEnumerable<SftpFile>> ListDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        return await Task.Run(() => _sftp.ListDirectory(path), ct);
    }

    public async Task DownloadFileAsync(string remotePath, string localPath, IProgress<ulong>? progress = null, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        await Task.Run(() =>
        {
            using var fs = File.OpenWrite(localPath);
        _sftp.DownloadFile(remotePath, fs, uploaded => progress?.Report(uploaded));
        }, ct);
    }

    public async Task UploadFileAsync(string localPath, string remotePath, IProgress<ulong>? progress = null, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        await Task.Run(() =>
        {
            using var fs = File.OpenRead(localPath);
            _sftp.UploadFile(fs, remotePath, uploaded => progress?.Report(uploaded));
        }, ct);
    }

    public async Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default)
    {
        if (_ssh is null) throw new InvalidOperationException("Not connected");
        return await Task.Run(() =>
        {
            using var cmd = _ssh.CreateCommand(command);
            var result = cmd.Execute();
            return string.IsNullOrWhiteSpace(result) ? cmd.Error : result;
        }, ct);
    }

    public async Task ChangeDirectoryAsync(string path, CancellationToken ct = default)
    {
        if (_sftp is null) throw new InvalidOperationException("Not connected");
        await Task.Run(() =>
        {
            var real = _sftp.GetAttributes(path); // validate
            _currentDir = path;
        }, ct);
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
