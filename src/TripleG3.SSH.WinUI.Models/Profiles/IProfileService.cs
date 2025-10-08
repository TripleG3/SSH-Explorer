using System;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.Profiles;

public interface IProfileService
{
    ProfileState State { get; }

    event Action<ProfileState> StateChanged;

    ValueTask CreateProfile(Profile profile);
    ValueTask DeleteProfile(string profileName);
    ValueTask LoadProfile(string profileName);
    ValueTask UpdateProfile(Profile profile);
}