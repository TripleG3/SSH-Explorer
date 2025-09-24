namespace SSHExplorer.Models;

public readonly record struct SshConnectionState(
    bool IsBusy, 
    bool IsConnected, 
    string RemotePath, 
    string LocalPath, 
    Profile? ConnectedProfile,
    string ErrorMessage)
{
    public static readonly SshConnectionState Empty = new(
        false, 
        false, 
        "/", 
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        null,
        string.Empty);
}