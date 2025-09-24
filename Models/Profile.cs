namespace SSHExplorer.Models;

public readonly record struct Profile(
    string Name,
    string Host,
    int Port,
    string Username,
    string? Password,
    string? PrivateKeyPath,
    string? Passphrase,
    string DefaultRemotePath,
    string DefaultLocalPath,
    SessionType SessionType,
    bool IsPinned)
{
    public static readonly Profile Empty = new(
        string.Empty,
        string.Empty,
        22,
        string.Empty,
        null,
        null,
        null,
        "/",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        SessionType.Ssh,
        false);

    public static Profile CreateLocal(string name, string defaultPath = "", bool isPinned = false)
    {
        var path = string.IsNullOrWhiteSpace(defaultPath) 
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : defaultPath;
            
        return new Profile(
            name,
            string.Empty,
            0,
            string.Empty,
            null,
            null,
            null,
            string.Empty,
            path,
            SessionType.Local,
            isPinned);
    }

    public static Profile CreateSsh(string name, string host, int port, string username, 
        string? password = null, string? privateKeyPath = null, string? passphrase = null, 
        string defaultRemotePath = "/", string defaultLocalPath = "", bool isPinned = false)
    {
        var localPath = string.IsNullOrWhiteSpace(defaultLocalPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : defaultLocalPath;
            
        return new Profile(
            name,
            host,
            port,
            username,
            password,
            privateKeyPath,
            passphrase,
            defaultRemotePath,
            localPath,
            SessionType.Ssh,
            isPinned);
    }

    public bool UseKeyAuth => !string.IsNullOrWhiteSpace(PrivateKeyPath);
    public bool IsLocal => SessionType == SessionType.Local;
    public bool IsSsh => SessionType == SessionType.Ssh;
}
