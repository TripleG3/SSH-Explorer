using Specky7;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[Singleton<IProfileService>]
public sealed class ProfileService : IProfileService
{
    public event Action<ProfileState> StateChanged = delegate { };

    private ProfileState state = ProfileState.Empty;

    public ProfileState State
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

    public async ValueTask CreateProfile(Profile profile)
    {
        State = State with { IsBusy = true };
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new ArgumentException("Profile name cannot be empty.");
        }
        string file = $"{profile.Name}.{ProfileConstants.ProfileExtension}";
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) != null)
        {
            throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists.");
        }
        var json = JsonSerializer.Serialize(profile);
        await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting), json);
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) == null)
        {
            throw new InvalidOperationException("Failed to create profile.");
        }
        State = State with { IsBusy = false };
    }

    public async ValueTask LoadProfile(string profileName)
    {
        State = State with { IsBusy = true };
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be empty.");
        }
        string file = $"{profileName}.{ProfileConstants.ProfileExtension}";
        var storageFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) as StorageFile ?? throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
        var json = await FileIO.ReadTextAsync(storageFile);
        var profile = JsonSerializer.Deserialize<Profile>(json) ?? throw new InvalidOperationException("Failed to load profile.");
        State = State with { IsBusy = false, Profile = profile };
    }

    public async ValueTask DeleteProfile(string profileName)
    {
        State = State with { IsBusy = true };
        if (string.IsNullOrWhiteSpace(profileName))
        {
            throw new ArgumentException("Profile name cannot be empty.");
        }
        string file = $"{profileName}.{ProfileConstants.ProfileExtension}";
        var storageFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) as StorageFile ?? throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
        await storageFile.DeleteAsync();
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) != null)
        {
            throw new InvalidOperationException("Failed to delete profile.");
        }
        if (State.Profile.Name == profileName)
        {
            State = State with { IsBusy = false, Profile = Profile.Empty };
        }
        else
        {
            State = State with { IsBusy = false };
        }
    }

    public async ValueTask UpdateProfile(Profile profile)
    {
        State = State with { IsBusy = true };
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            throw new ArgumentException("Profile name cannot be empty.");
        }
        string file = $"{profile.Name}.{ProfileConstants.ProfileExtension}";
        var storageFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) as StorageFile ?? throw new InvalidOperationException($"Profile '{profile.Name}' does not exist.");
        var json = JsonSerializer.Serialize(profile);
        await FileIO.WriteTextAsync(storageFile, json);
        if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(file) == null)
        {
            throw new InvalidOperationException("Failed to update profile.");
        }
        if (State.Profile.Name == profile.Name)
        {
            State = State with { IsBusy = false, Profile = profile };
        }
        else
        {
            State = State with { IsBusy = false };
        }
    }
}
