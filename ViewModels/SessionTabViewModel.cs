using System.ComponentModel;
using System.Windows.Input;
using SSHExplorer.Models;
using SSHExplorer.Models.Services;

namespace SSHExplorer.ViewModels;

public sealed class SessionTabViewModel : INotifyPropertyChanged
{
    private readonly ISessionService _sessionService;
    private readonly IProfileService _profileService;

    public SessionTabViewModel(
        ISessionService sessionService,
        IProfileService profileService)
    {
        _sessionService = sessionService;
        _profileService = profileService;

        // Subscribe to state changes
        _sessionService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SessionState)));
        _profileService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfileState)));

        // Initialize commands
        CreateLocalSessionCommand = new AsyncBindingCommand(async _ => await CreateLocalSessionAsync(CancellationToken.None), _ => !SessionState.IsBusy, this);
        CreateSshSessionCommand = new AsyncBindingCommand(async _ => await CreateSshSessionAsync(CancellationToken.None), _ => !SessionState.IsBusy && ProfileState.SelectedProfile.HasValue, this);
        CloseSessionCommand = new AsyncBindingCommand<string>(async sessionId => await CloseSessionAsync(sessionId ?? string.Empty, CancellationToken.None));
        SetActiveSessionCommand = new AsyncBindingCommand<string>(async sessionId => await SetActiveSessionAsync(sessionId ?? string.Empty, CancellationToken.None));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SessionState SessionState => _sessionService.State;
    public ProfileState ProfileState => _profileService.State;

    public ICommand CreateLocalSessionCommand { get; }
    public ICommand CreateSshSessionCommand { get; }
    public ICommand CloseSessionCommand { get; }
    public ICommand SetActiveSessionCommand { get; }

    private async Task CreateLocalSessionAsync(CancellationToken ct)
    {
        var name = $"Local Session {SessionState.Sessions.Count + 1}";
        await _sessionService.CreateLocalSessionAsync(name, null, ct);
    }

    private async Task CreateSshSessionAsync(CancellationToken ct)
    {
        if (!ProfileState.SelectedProfile.HasValue)
            return;

        await _sessionService.CreateSshSessionAsync(ProfileState.SelectedProfile.Value, ct);
    }

    private async Task CloseSessionAsync(string sessionId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _sessionService.CloseSessionAsync(sessionId, ct);
        }
    }

    private async Task SetActiveSessionAsync(string sessionId, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _sessionService.SetActiveSessionAsync(sessionId, ct);
        }
    }
}