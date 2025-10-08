using System.Collections.Immutable;

namespace TripleG3.SSH.WinUI.Models.Profiles;

public record ProfilesState(ImmutableList<Profile> Profiles, bool IsBusy)
{
    public static ProfilesState Empty { get; } = new ProfilesState([], false);
}
