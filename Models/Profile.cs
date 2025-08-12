namespace SSHExplorer.Models;

public class Profile
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string? Passphrase { get; set; }
    public string DefaultRemotePath { get; set; } = "/";
    public string DefaultLocalPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public bool UseKeyAuth => !string.IsNullOrWhiteSpace(PrivateKeyPath);
}
