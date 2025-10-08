using System;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.Profiles
{
    public interface IProfilesService
    {
        ProfilesState State { get; }

        event Action<ProfilesState> StateChanged;

        ValueTask DeleteAllProfiles();
        ValueTask LoadProfiles();
    }
}