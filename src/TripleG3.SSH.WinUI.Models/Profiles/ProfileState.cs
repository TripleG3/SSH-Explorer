using System.Diagnostics.CodeAnalysis;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[ExcludeFromCodeCoverage(Justification = "Data Model")]
public record ProfileState(Profile Profile, bool IsBusy)
{
    public static ProfileState Empty { get; } = new ProfileState(Profile.Empty, false);
}
