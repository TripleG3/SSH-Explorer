using Specky7;
using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[Singleton<IProfilesService>]
public sealed class ProfilesService : IProfilesService
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
        var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
        var profiles = ImmutableList.CreateBuilder<Profile>();
        foreach (var file in files)
        {
            if (file.FileType.Equals($".{ProfileConstants.ProfileExtension}", StringComparison.OrdinalIgnoreCase))
            {
                var json = await FileIO.ReadTextAsync(file);
                var profile = JsonSerializer.Deserialize<Profile>(json);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }
        }
        State = new ProfilesState(profiles.ToImmutable(), false);
    }

    public async ValueTask DeleteAllProfiles()
    {
        State = State with { IsBusy = true };
        var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
        foreach (var file in files)
        {
            if (file.FileType.Equals($".{ProfileConstants.ProfileExtension}", StringComparison.OrdinalIgnoreCase))
            {
                await file.DeleteAsync();
            }
        }
        State = new ProfilesState([], false);
    }
}