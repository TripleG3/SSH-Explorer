namespace SSHExplorer.Models;

public readonly record struct Session(
    string Id,
    string Name,
    SessionType Type,
    Profile? Profile,
    string CurrentPath,
    bool IsConnected,
    DateTime CreatedAt)
{
    public static Session CreateLocal(string name, string path = "")
    {
        var initialPath = string.IsNullOrWhiteSpace(path) 
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : path;
            
        return new Session(
            Guid.NewGuid().ToString(),
            name,
            SessionType.Local,
            null,
            initialPath,
            true,
            DateTime.Now);
    }
    
    public static Session CreateSsh(Profile profile)
    {
        return new Session(
            Guid.NewGuid().ToString(),
            profile.Name,
            SessionType.Ssh,
            profile,
            profile.DefaultRemotePath,
            false,
            DateTime.Now);
    }
}