using System.ComponentModel;
using System.Windows.Input;
using SSHExplorer.Models;
using SSHExplorer.Models.Services;

namespace SSHExplorer.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISessionService _sessionService;
    private readonly IProfileService _profileService;
    private SessionTabViewModel? _sessionTabViewModel;

    public MainViewModel(
        ISessionService sessionService,
        IProfileService profileService)
    {
        _sessionService = sessionService;
        _profileService = profileService;

        // Subscribe to state changes
        _sessionService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionState)));
        _profileService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfileState)));

        // Initialize commands
        LoadCommand = new AsyncBindingCommand(_ => LoadAsync(), _ => !ProfileState.IsBusy, this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // State properties
    public SessionState SessionState => _sessionService.State;
    public ProfileState ProfileState => _profileService.State;

    // Commands
    public ICommand LoadCommand { get; }

    // Get the SessionTabViewModel for the view
    public SessionTabViewModel SessionTabViewModel
    {
        get
        {
            _sessionTabViewModel ??= new SessionTabViewModel(_sessionService, _profileService);
            return _sessionTabViewModel;
        }
    }

    private async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            await _profileService.LoadAsync(ct);
            
            // Try to restore the last used profile
            var lastProfileName = Preferences.Get("LastProfileName", string.Empty);
            if (ProfileState.Profiles.Count == 0)
            {
                // Create some default profiles for testing
                await CreateDefaultProfilesAsync(ct);
            }
            else if (ProfileState.SelectedProfile is null)
            {
                // Restore last used profile if available
                var profile = ProfileState.Profiles.FirstOrDefault(p => 
                    string.Equals(p.Name, lastProfileName, StringComparison.OrdinalIgnoreCase));
                if (profile.Equals(Profile.Empty))
                    profile = ProfileState.Profiles.First();
                
                await _profileService.SelectProfileAsync(profile, ct);
            }

            // Auto-create a default local session for convenience
            if (SessionState.Sessions.Count == 0)
            {
                await _sessionService.CreateLocalSessionAsync("Local Files", null, ct);
            }
        }
        catch (Exception ex)
        {
            // Handle load error - in a real app you might want to use a dialog service
            System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
        }
    }

    private async Task CreateDefaultProfilesAsync(CancellationToken ct = default)
    {
        try
        {
            // Create a default local profile
            var localProfile = Profile.CreateLocal("Local Files", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            await _profileService.AddOrUpdateAsync(localProfile, ct);

            // Create a sample SSH profile (user can modify later)
            var sshProfile = Profile.CreateSsh("Sample SSH", "localhost", 22, "user");
            await _profileService.AddOrUpdateAsync(sshProfile, ct);

            await _profileService.SelectProfileAsync(localProfile, ct);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Default profile creation error: {ex.Message}");
        }
    }
}
