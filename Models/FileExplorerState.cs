using Renci.SshNet.Sftp;
using System.Collections.ObjectModel;

namespace SSHExplorer.Models;

public readonly record struct FileExplorerState(
    bool IsBusy,
    ObservableCollection<SftpFile> RemoteItems,
    ObservableCollection<FileSystemInfo> LocalItems,
    SftpFile? SelectedRemoteItem,
    FileSystemInfo? SelectedLocalItem,
    string ErrorMessage)
{
    public static readonly FileExplorerState Empty = new(
        false,
        new ObservableCollection<SftpFile>(),
        new ObservableCollection<FileSystemInfo>(),
        null,
        null,
        string.Empty);
}