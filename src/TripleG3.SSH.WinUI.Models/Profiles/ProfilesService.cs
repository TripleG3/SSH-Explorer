using Specky7;
using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[Singleton<IProfilesService>]
public sealed class ProfilesService(IProfileStorage storage) : IProfilesService
{
    public event Action<ProfilesState> StateChanged = delegate { };
    private ProfilesState state = ProfilesState.Empty;

    public ProfilesState State
    {
        get => state;
        private set
        {
            if (state != value)
            {
                state = value;
                StateChanged(state);
            }
        }
    }

    public async ValueTask LoadProfiles()
    {
        State = State with { IsBusy = true };
        try
        {
            var files = await storage.EnumerateAsync($".{ProfileConstants.ProfileExtension}");
            var profiles = ImmutableList.CreateBuilder<Profile>();
            foreach (var file in files)
            {
                var json = await storage.ReadAsync(file);
                var profile = JsonSerializer.Deserialize<Profile>(json);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }
            // Emit final state with IsBusy=false; finally will attempt to set busy=false again but it will be a no-op
            State = new ProfilesState(profiles.ToImmutable(), false);
        }
        finally
        {
            State = State with { IsBusy = false };
        }
    }

    public async ValueTask DeleteAllProfiles()
    {
        State = State with { IsBusy = true };
        try
        {
            var files = await storage.EnumerateAsync($".{ProfileConstants.ProfileExtension}");
            foreach (var file in files)
            {
                await storage.DeleteAsync(file);
            }
            // Emit final state with IsBusy=false; finally will ensure busy is false even on failure
            State = new ProfilesState([], false);
        }
        finally
        {
            State = State with { IsBusy = false };
        }
    }
}