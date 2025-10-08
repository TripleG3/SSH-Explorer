namespace TripleG3.SSH.WinUI.Models.Profiles;

public record ProfileState(Profile Profile, bool IsBusy)
{
    public static ProfileState Empty { get; } = new ProfileState(Profile.Empty, false);
}
