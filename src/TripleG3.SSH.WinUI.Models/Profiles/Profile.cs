namespace TripleG3.SSH.WinUI.Models.Profiles;

public record Profile(string Name, string Address, string Username, string Password, int Port)
{
    public static Profile Empty { get; } = new Profile(string.Empty, string.Empty, string.Empty, string.Empty, 0);
}
