using System.Diagnostics.CodeAnalysis;

namespace TripleG3.SSH.WinUI.Models.SSH;

[ExcludeFromCodeCoverage(Justification = "Data model")]
public record SSHState(Profiles.Profile Profile, bool IsConnected, bool IsBusy)
{
    public static SSHState Empty { get; } = new SSHState(Profiles.Profile.Empty, false, false);
}
