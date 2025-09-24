using SSHExplorer.Models;
using Renci.SshNet.Sftp;
using System.Collections.ObjectModel;

namespace SSHExplorer.Models.Services;

public sealed class FileExplorerService : StatePublisher<FileExplorerState>, IFileExplorerService
{
    private readonly ISshService _sshService;

    public FileExplorerService(ISshService sshService) : base(FileExplorerState.Empty)
    {
        _sshService = sshService;
    }

    public async Task RefreshRemoteAsync(string remotePath, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });
        
        try
        {
            var items = await _sshService.ListDirectoryAsync(remotePath, ct);
            var filteredItems = items.Where(item => item.Name is not ("." or "..")).ToList();
            var remoteItems = new ObservableCollection<SftpFile>(filteredItems);
            
            SetState(State with 
            { 
                IsBusy = false, 
                RemoteItems = remoteItems, 
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task RefreshLocalAsync(string localPath, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });
        
        try
        {
            var localItems = await Task.Run(() =>
            {
                var items = new List<FileSystemInfo>();
                var di = new DirectoryInfo(localPath);
                
                if (di.Exists)
                {
                    foreach (var item in di.EnumerateFileSystemInfos())
                    {
                        items.Add(item);
                    }
                }
                return items;
            }, ct);

            SetState(State with 
            { 
                IsBusy = false, 
                LocalItems = new ObservableCollection<FileSystemInfo>(localItems), 
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task SelectRemoteItemAsync(SftpFile? item, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Make it properly async
        SetState(State with { SelectedRemoteItem = item });
    }

    public async Task SelectLocalItemAsync(FileSystemInfo? item, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Make it properly async
        SetState(State with { SelectedLocalItem = item });
    }

    public async Task NavigateToLocalAsync(string localPath, CancellationToken ct = default)
    {
        SetState(State with { LocalPath = localPath });
        await RefreshLocalAsync(localPath, ct);
    }

    public async Task NavigateToRemoteAsync(string remotePath, CancellationToken ct = default)
    {
        SetState(State with { RemotePath = remotePath });
        await RefreshRemoteAsync(remotePath, ct);
    }
}
