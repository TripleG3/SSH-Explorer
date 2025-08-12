using Renci.SshNet;
using Renci.SshNet.Sftp;
using SSHExplorer.Models;

namespace SSHExplorer.Services;

public interface ISshService : IAsyncDisposable
{
    Task ConnectAsync(Profile profile, CancellationToken ct = default);
    bool IsConnected { get; }
    Task<IEnumerable<SftpFile>> ListDirectoryAsync(string path, CancellationToken ct = default);
    Task DownloadFileAsync(string remotePath, string localPath, IProgress<ulong>? progress = null, CancellationToken ct = default);
    Task UploadFileAsync(string localPath, string remotePath, IProgress<ulong>? progress = null, CancellationToken ct = default);
    Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default);
    Task ChangeDirectoryAsync(string path, CancellationToken ct = default);
    string CurrentDirectory { get; }
}
