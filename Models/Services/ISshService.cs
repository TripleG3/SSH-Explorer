using Renci.SshNet.Sftp;

namespace SSHExplorer.Models.Services;

public interface ISshService : IStatePublisher<SshConnectionState>, IAsyncDisposable
{
    Task ConnectAsync(Profile profile, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<IEnumerable<SftpFile>> ListDirectoryAsync(string path, CancellationToken ct = default);
    Task DownloadFileAsync(string remotePath, string localPath, IProgress<ulong>? progress = null, CancellationToken ct = default);
    Task UploadFileAsync(string localPath, string remotePath, IProgress<ulong>? progress = null, CancellationToken ct = default);
    Task<string> ExecuteCommandAsync(string command, CancellationToken ct = default);
    Task ChangeDirectoryAsync(string path, CancellationToken ct = default);
}
