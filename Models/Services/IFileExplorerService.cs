using Renci.SshNet.Sftp;

namespace SSHExplorer.Models.Services;

public interface IFileExplorerService : IStatePublisher<FileExplorerState>
{
    Task RefreshRemoteAsync(string remotePath, CancellationToken ct = default);
    Task RefreshLocalAsync(string localPath, CancellationToken ct = default);
    Task SelectRemoteItemAsync(SftpFile? item, CancellationToken ct = default);
    Task SelectLocalItemAsync(FileSystemInfo? item, CancellationToken ct = default);
    Task NavigateToLocalAsync(string localPath, CancellationToken ct = default);
    Task NavigateToRemoteAsync(string remotePath, CancellationToken ct = default);
}
