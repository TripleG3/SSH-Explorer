using System.ComponentModel;
using System.Windows.Input;
using SSHExplorer.Models;
using SSHExplorer.Models.Services;

namespace SSHExplorer.ViewModels;

public sealed class SshConnectionToolbarViewModel : INotifyPropertyChanged
{
    private readonly ISshService _sshService;
    private readonly IProfileService _profileService;
    private readonly ITerminalService _terminalService;
    private readonly IUiInteractionService _uiInteractionService;

    public SshConnectionToolbarViewModel(
        ISshService sshService,
        IProfileService profileService, 
        ITerminalService terminalService,
        IUiInteractionService uiInteractionService)
    {
        _sshService = sshService;
        _profileService = profileService;
        _terminalService = terminalService;
        _uiInteractionService = uiInteractionService;

        // Subscribe to state changes
        _sshService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        _profileService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
        _terminalService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));

        ConnectCommand = new AsyncBindingCommand(_ => ConnectAsync(), _ => !State.SshState.IsBusy, this);
        DisconnectCommand = new AsyncBindingCommand(_ => DisconnectAsync(), _ => State.SshState.IsConnected && !State.SshState.IsBusy, this);
        ToggleTerminalCommand = new AsyncBindingCommand(_ => _terminalService.ToggleVisibilityAsync(), _ => true, this);
        ShowCommandsCommand = new AsyncBindingCommand(_ => ShowCommandsAsync(), _ => true, this);
        ShowAboutCommand = new AsyncBindingCommand(_ => _uiInteractionService.ShowAboutDialogAsync(), _ => true, this);
        NavigateToOptionsCommand = new AsyncBindingCommand(_ => _uiInteractionService.NavigateToOptionsAsync(), _ => true, this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // State property combining multiple service states
    public SshConnectionToolbarState State => new(
        _sshService.State,
        _profileService.State,
        _terminalService.State.IsVisible
    );

    // Commands
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ToggleTerminalCommand { get; }
    public ICommand ShowCommandsCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand NavigateToOptionsCommand { get; }

    private async Task ConnectAsync()
    {
        if (!State.ProfileState.SelectedProfile.HasValue) return;
        
        var selectedProfile = State.ProfileState.SelectedProfile.Value;
        await _sshService.ConnectAsync(selectedProfile);
    }

    private async Task DisconnectAsync()
    {
        await _sshService.DisconnectAsync();
    }

    private async Task ShowCommandsAsync()
    {
        // This could be expanded to show commands specific to the toolbar
        await _uiInteractionService.ShowCommandsActionSheetAsync();
    }
}

// State model for the toolbar
public readonly record struct SshConnectionToolbarState(
    SshConnectionState SshState,
    ProfileState ProfileState,
    bool IsTerminalVisible)
{
    public static readonly SshConnectionToolbarState Empty = new(
        SshConnectionState.Empty,
        ProfileState.Empty,
        false);
}