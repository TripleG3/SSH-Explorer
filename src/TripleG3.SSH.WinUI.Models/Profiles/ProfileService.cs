using Specky7;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace TripleG3.SSH.WinUI.Models.Profiles;

[Singleton<IProfileService>]
public sealed class ProfileService(IProfileStorage storage) : IProfileService
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
        try
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new ArgumentException("Profile name cannot be empty.");
            }
            string file = $"{profile.Name}.{ProfileConstants.ProfileExtension}";
            if (await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException($"A profile with the name '{profile.Name}' already exists.");
            }
            var json = JsonSerializer.Serialize(profile);
            await storage.CreateOrReplaceAsync(file, json);
            if (!await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException("Failed to create profile.");
            }
        }
        finally
        {
            State = State with { IsBusy = false };
        }
    }

    public async ValueTask LoadProfile(string profileName)
    {
        State = State with { IsBusy = true };
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                throw new ArgumentException("Profile name cannot be empty.");
            }
            string file = $"{profileName}.{ProfileConstants.ProfileExtension}";
            if (!await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
            }
            var json = await storage.ReadAsync(file);
            var profile = JsonSerializer.Deserialize<Profile>(json) ?? throw new InvalidOperationException("Failed to load profile.");
            State = State with { Profile = profile };
        }
        finally
        {
            State = State with { IsBusy = false };
        }
    }

    public async ValueTask DeleteProfile(string profileName)
    {
        State = State with { IsBusy = true };
        try
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                throw new ArgumentException("Profile name cannot be empty.");
            }
            string file = $"{profileName}.{ProfileConstants.ProfileExtension}";
            if (!await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
            }
            await storage.DeleteAsync(file);
            if (await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException("Failed to delete profile.");
            }
            if (State.Profile.Name == profileName)
            {
                State = State with { Profile = Profile.Empty };
            }
        }
        finally
        {
            State = State with { IsBusy = false };
        }
    }

    public async ValueTask UpdateProfile(Profile profile)
    {
        State = State with { IsBusy = true };
        try
        {
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                throw new ArgumentException("Profile name cannot be empty.");
            }
            string file = $"{profile.Name}.{ProfileConstants.ProfileExtension}";
            if (!await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException($"Profile '{profile.Name}' does not exist.");
            }
            var json = JsonSerializer.Serialize(profile);
            await storage.CreateOrReplaceAsync(file, json);
            if (!await storage.ExistsAsync(file))
            {
                throw new InvalidOperationException("Failed to update profile.");
            }
            if (State.Profile.Name == profile.Name)
            {
                State = State with { Profile = profile };
            }
        }
        finally
        {
            State = State with { IsBusy = false };
        }
    }
}
