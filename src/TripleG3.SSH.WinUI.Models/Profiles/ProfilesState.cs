using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[ExcludeFromCodeCoverage(Justification = "Data Model")]
public record ProfilesState(ImmutableList<Profile> Profiles, bool IsBusy)
{
    public static ProfilesState Empty { get; } = new ProfilesState([], false);
}
