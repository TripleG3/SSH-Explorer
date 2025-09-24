using System.Text.Json;
using SSHExplorer.Models;
using System.Collections.ObjectModel;

namespace SSHExplorer.Services;

public sealed class ProfileService : StatePublisher<ProfileState>, IProfileService
{
    private readonly string _storePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ProfileService() : base(ProfileState.Empty)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "TripleG3", "SSHExplorer");
        Directory.CreateDirectory(dir);
        _storePath = Path.Combine(dir, "profiles.json");
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });
        
        try
        {
            IReadOnlyList<Profile> profiles = Array.Empty<Profile>();
            
            if (File.Exists(_storePath))
            {
                await using var fs = File.OpenRead(_storePath);
                profiles = await JsonSerializer.DeserializeAsync<List<Profile>>(fs, cancellationToken: ct) ?? new();
            }

            var observableProfiles = new ObservableCollection<Profile>(profiles);
            SetState(State with { IsBusy = false, Profiles = observableProfiles, ErrorMessage = string.Empty });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
            throw;
        }
    }

    public async Task SaveAsync(IEnumerable<Profile> profiles, CancellationToken ct = default)
    {
        try
        {
            await using var fs = File.Create(_storePath);
            await JsonSerializer.SerializeAsync(fs, profiles, _jsonOptions, ct);
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Save failed: {ex.Message}" });
            throw;
        }
    }

    public async Task AddOrUpdateAsync(Profile profile, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true });
        
        try
        {
            var profiles = State.Profiles.ToList();
            var existing = profiles.FindIndex(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0) 
                profiles[existing] = profile; 
            else 
                profiles.Add(profile);
            
            await SaveAsync(profiles, ct);
            
            var updatedProfiles = new ObservableCollection<Profile>(profiles);
            SetState(State with { IsBusy = false, Profiles = updatedProfiles, ErrorMessage = string.Empty });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
            throw;
        }
    }

    public async Task DeleteAsync(string name, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true });
        
        try
        {
            var profiles = State.Profiles.Where(p => !string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
            await SaveAsync(profiles, ct);
            
            var updatedProfiles = new ObservableCollection<Profile>(profiles);
            var selectedProfile = State.SelectedProfile?.Name == name ? null : State.SelectedProfile;
            
            SetState(State with 
            { 
                IsBusy = false, 
                Profiles = updatedProfiles, 
                SelectedProfile = selectedProfile,
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
            throw;
        }
    }

    public async Task SelectProfileAsync(Profile? profile, CancellationToken ct = default)
    {
        await Task.Run(() => 
        {
            SetState(State with { SelectedProfile = profile });
            if (profile is not null)
            {
                Preferences.Set("LastProfileName", profile.Name);
            }
        }, ct);
    }
}
