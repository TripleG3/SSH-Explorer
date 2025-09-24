using System.ComponentModel;
using System.Windows.Input;
using SSHExplorer.Models;
using SSHExplorer.Services;
using SSHExplorer.Utilities;
using Renci.SshNet.Sftp;

namespace SSHExplorer.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISshService _sshService;
    private readonly IProfileService _profileService;
    private readonly IFileExplorerService _fileExplorerService;
    private readonly ITerminalService _terminalService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private readonly IUiInteractionService _uiInteractionService;

    public MainViewModel(
        ISshService sshService,
        IProfileService profileService,
        IFileExplorerService fileExplorerService,
        ITerminalService terminalService,
        IThemeService themeService,
        IDialogService dialogService,
        IUiInteractionService uiInteractionService)
    {
        _sshService = sshService;
        _profileService = profileService;
        _fileExplorerService = fileExplorerService;
        _terminalService = terminalService;
        _themeService = themeService;
        _dialogService = dialogService;
        _uiInteractionService = uiInteractionService;

        // Subscribe to state changes
        _sshService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SshState)));
        _profileService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProfileState)));
        _fileExplorerService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileExplorerState)));
        _terminalService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TerminalState)));
        _uiInteractionService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UiInteractionState)));

        // Initialize commands
        LoadCommand = new AsyncBindingCommand(_ => LoadAsync(), _ => !ProfileState.IsBusy, this);
        ConnectCommand = new AsyncBindingCommand(_ => ConnectAsync(), _ => !SshState.IsBusy, this);
        DisconnectCommand = new AsyncBindingCommand(_ => DisconnectAsync(), _ => SshState.IsConnected && !SshState.IsBusy, this);
        RefreshRemoteCommand = new AsyncBindingCommand(_ => RefreshRemoteAsync(), _ => SshState.IsConnected && !FileExplorerState.IsBusy, this);
        RefreshLocalCommand = new AsyncBindingCommand(_ => RefreshLocalAsync(), _ => !FileExplorerState.IsBusy, this);
        ExecuteCommand = new AsyncBindingCommand(_ => ExecuteAsync(), _ => SshState.IsConnected && !TerminalState.IsBusy && !string.IsNullOrWhiteSpace(TerminalState.Input), this);
        ToggleTerminalCommand = new AsyncBindingCommand(_ => _terminalService.ToggleVisibilityAsync(), _ => true, this);
        TogglePinCommand = new AsyncBindingCommand(_ => _terminalService.TogglePinAsync(), _ => true, this);
        ToggleThemeCommand = new AsyncBindingCommand(_ => ToggleThemeAsync(), _ => true, this);
        NavigateRemoteCommand = new AsyncBindingCommand<SftpFile>(item => NavigateRemoteAsync(item!), _ => SshState.IsConnected, this);
        NavigateLocalCommand = new AsyncBindingCommand<FileSystemInfo>(item => NavigateLocalAsync(item!), _ => true, this);
        GoBackRemoteCommand = new AsyncBindingCommand(_ => GoBackRemoteAsync(), _ => SshState.IsConnected, this);
        GoBackLocalCommand = new AsyncBindingCommand(_ => GoBackLocalAsync(), _ => true, this);
        
        // UI Interaction Commands
        StartRemoteDragCommand = new AsyncBindingCommand<SftpFile>(file => StartRemoteDragAsync(file!), _ => SshState.IsConnected, this);
        StartLocalDragCommand = new AsyncBindingCommand<FileSystemInfo>(file => StartLocalDragAsync(file!), _ => true, this);
        DropOnRemoteCommand = new AsyncBindingCommand(_ => DropOnRemoteAsync(), _ => !UiInteractionState.IsBusy, this);
        DropOnLocalCommand = new AsyncBindingCommand(_ => DropOnLocalAsync(), _ => !UiInteractionState.IsBusy, this);
        StartTerminalResizeCommand = new AsyncBindingCommand<double>(height => StartTerminalResizeAsync(height), _ => true, this);
        UpdateTerminalHeightCommand = new AsyncBindingCommand<double>(deltaY => UpdateTerminalHeightAsync(deltaY), _ => UiInteractionState.TerminalResize.IsResizing, this);
        EndTerminalResizeCommand = new AsyncBindingCommand(_ => EndTerminalResizeAsync(), _ => UiInteractionState.TerminalResize.IsResizing, this);
        ShowCommandsCommand = new AsyncBindingCommand(_ => ShowCommandsAsync(), _ => true, this);
        ShowAboutCommand = new AsyncBindingCommand(_ => _uiInteractionService.ShowAboutDialogAsync(), _ => true, this);
        NavigateToOptionsCommand = new AsyncBindingCommand(_ => _uiInteractionService.NavigateToOptionsAsync(), _ => true, this);
        RegisterKeyboardAcceleratorsCommand = new AsyncBindingCommand(_ => _uiInteractionService.RegisterKeyboardAcceleratorsAsync(), _ => true, this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // State properties
    public SshConnectionState SshState => _sshService.State;
    public ProfileState ProfileState => _profileService.State;
    public FileExplorerState FileExplorerState => _fileExplorerService.State;
    public TerminalState TerminalState => _terminalService.State;
    public UiInteractionState UiInteractionState => _uiInteractionService.State;

    // Commands
    public ICommand LoadCommand { get; }
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand RefreshRemoteCommand { get; }
    public ICommand RefreshLocalCommand { get; }
    public ICommand ExecuteCommand { get; }
    public ICommand ToggleTerminalCommand { get; }
    public ICommand TogglePinCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand NavigateRemoteCommand { get; }
    public ICommand NavigateLocalCommand { get; }
    public ICommand GoBackRemoteCommand { get; }
    public ICommand GoBackLocalCommand { get; }
    
    // UI Interaction Commands
    public ICommand StartRemoteDragCommand { get; }
    public ICommand StartLocalDragCommand { get; }
    public ICommand DropOnRemoteCommand { get; }
    public ICommand DropOnLocalCommand { get; }
    public ICommand StartTerminalResizeCommand { get; }
    public ICommand UpdateTerminalHeightCommand { get; }
    public ICommand EndTerminalResizeCommand { get; }
    public ICommand ShowCommandsCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand NavigateToOptionsCommand { get; }
    public ICommand RegisterKeyboardAcceleratorsCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            await _profileService.LoadAsync();
            
            // Try to restore the last used profile
            var lastProfileName = Preferences.Get("LastProfileName", string.Empty);
            if (ProfileState.Profiles.Count == 0)
            {
                // Prompt for first profile
                var created = await CreateProfileInteractiveAsync();
                if (created is not null)
                {
                    await _profileService.AddOrUpdateAsync(created);
                    await _profileService.SelectProfileAsync(created);
                }
            }
            else if (ProfileState.SelectedProfile is null)
            {
                // Restore last used profile if available
                var profile = ProfileState.Profiles.FirstOrDefault(p => 
                    string.Equals(p.Name, lastProfileName, StringComparison.OrdinalIgnoreCase)) 
                    ?? ProfileState.Profiles.First();
                
                await _profileService.SelectProfileAsync(profile);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayMessageAsync("Load Error", $"Failed to load profiles: {ex.Message}");
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            // If no profile selected, guide the user to create one
            if (ProfileState.SelectedProfile is null)
            {
                var created = await CreateProfileInteractiveAsync();
                if (created is null) return; // user cancelled
                
                await _profileService.AddOrUpdateAsync(created);
                await _profileService.SelectProfileAsync(created);
            }

            await _sshService.ConnectAsync(ProfileState.SelectedProfile!);
            await _terminalService.AppendOutputAsync($"Connected to {ProfileState.SelectedProfile!.Host}\n");
            
            // Refresh file explorers
            await RefreshRemoteAsync();
            await RefreshLocalAsync();
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Connection failed: {ex.Message}\n");
            await _dialogService.DisplayMessageAsync("Connection Error", $"Failed to connect: {ex.Message}");
        }
    }

    private async Task DisconnectAsync()
    {
        try
        {
            var ok = await _dialogService.DisplayAlertAsync("Disconnect", "Disconnect from current SSH session?", "OK", "Cancel");
            if (!ok) return;

            await _sshService.DisconnectAsync();
            await _terminalService.AppendOutputAsync("Disconnected\n");
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Disconnect error: {ex.Message}\n");
        }
    }

    private async Task RefreshRemoteAsync()
    {
        if (!SshState.IsConnected) return;
        
        try
        {
            await _fileExplorerService.RefreshRemoteAsync(SshState.RemotePath);
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Remote refresh failed: {ex.Message}\n");
            await _dialogService.DisplayMessageAsync("Browse Error", $"Could not load remote folder: {ex.Message}");
        }
    }

    private async Task RefreshLocalAsync()
    {
        try
        {
            await _fileExplorerService.RefreshLocalAsync(SshState.LocalPath);
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Local refresh failed: {ex.Message}\n");
        }
    }

    private async Task ExecuteAsync()
    {
        if (!SshState.IsConnected || string.IsNullOrWhiteSpace(TerminalState.Input)) return;

        try
        {
            await _terminalService.AppendOutputAsync($"> {TerminalState.Input}\n");
            var result = await _sshService.ExecuteCommandAsync(TerminalState.Input);
            await _terminalService.AppendOutputAsync(result + "\n");
            await _terminalService.SetInputAsync(string.Empty);
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Command failed: {ex.Message}\n");
        }
    }

    public async Task ExecuteCommandAsync(string command)
    {
        if (!SshState.IsConnected || string.IsNullOrWhiteSpace(command)) return;

        try
        {
            await _terminalService.AppendOutputAsync($"> {command}\n");
            var result = await _sshService.ExecuteCommandAsync(command);
            await _terminalService.AppendOutputAsync(result + "\n");
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Command failed: {ex.Message}\n");
        }
    }

    private async Task NavigateRemoteAsync(SftpFile item)
    {
        if (item.IsDirectory)
        {
            try
            {
                var newPath = PathHelpers.CombineUnix(SshState.RemotePath, item.Name);
                await _sshService.ChangeDirectoryAsync(newPath);
                await RefreshRemoteAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                // Permission denied - show a user-friendly toast/message
                await _terminalService.AppendOutputAsync($"Access denied: {ex.Message}\n");
                await _dialogService.DisplayMessageAsync("Access Denied", ex.Message);
            }
            catch (Exception ex)
            {
                await _terminalService.AppendOutputAsync($"Navigation failed: {ex.Message}\n");
                await _dialogService.DisplayMessageAsync("Navigation Error", $"Could not open folder: {ex.Message}");
            }
        }
        else
        {
            // Show file actions
            await ShowRemoteFileActionsAsync(item);
        }
    }

    private async Task NavigateLocalAsync(FileSystemInfo item)
    {
        if (item is DirectoryInfo directory)
        {
            try
            {
                await _fileExplorerService.RefreshLocalAsync(directory.FullName);
            }
            catch (Exception ex)
            {
                await _terminalService.AppendOutputAsync($"Local navigation failed: {ex.Message}\n");
                await _dialogService.DisplayMessageAsync("Navigation Error", $"Could not open folder: {ex.Message}");
            }
        }
        else
        {
            // Show local file actions
            await ShowLocalFileActionsAsync(item);
        }
    }

    private async Task GoBackRemoteAsync()
    {
        try
        {
            var parentPath = PathHelpers.ParentUnix(SshState.RemotePath);
            if (parentPath != SshState.RemotePath)
            {
                await _sshService.ChangeDirectoryAsync(parentPath);
                await RefreshRemoteAsync();
            }
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Navigation failed: {ex.Message}\n");
        }
    }

    private async Task GoBackLocalAsync()
    {
        try
        {
            var parent = Directory.GetParent(SshState.LocalPath)?.FullName;
            if (!string.IsNullOrEmpty(parent))
            {
                await _fileExplorerService.RefreshLocalAsync(parent);
            }
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Navigation failed: {ex.Message}\n");
        }
    }

    private async Task ToggleThemeAsync()
    {
        try
        {
            var current = Application.Current!.RequestedTheme;
            
            if (current == AppTheme.Dark)
                await _themeService.SetThemeAsync(AppTheme.Light);
            else
                await _themeService.SetThemeAsync(AppTheme.Dark);
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Theme change failed: {ex.Message}\n");
        }
    }

    private async Task<Profile?> CreateProfileInteractiveAsync()
    {
        try
        {
            var name = await _dialogService.DisplayPromptAsync("Create Profile", "Enter a profile name:", "OK", "Cancel", "Default");
            if (string.IsNullOrWhiteSpace(name)) name = "Default";

            var host = await _dialogService.DisplayPromptAsync("Create Profile", "Host name or IP:", "OK", "Cancel");
            if (string.IsNullOrWhiteSpace(host)) return null;

            var user = await _dialogService.DisplayPromptAsync("Create Profile", "Username:", "OK", "Cancel");
            if (string.IsNullOrWhiteSpace(user)) user = Environment.UserName;

            var portStr = await _dialogService.DisplayPromptAsync("Create Profile", "Port:", "OK", "Cancel", initialValue: "22");
            var pass = await _dialogService.DisplayPromptAsync("Create Profile", "Password (leave blank if using key):", "OK", "Cancel");

            return new Profile
            {
                Name = name!,
                Host = host!,
                Username = user!,
                Password = string.IsNullOrEmpty(pass) ? null : pass,
                Port = int.TryParse(portStr, out var portVal) ? portVal : 22
            };
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"Profile creation failed: {ex.Message}\n");
            return null;
        }
    }

    private async Task ShowRemoteFileActionsAsync(SftpFile file)
    {
        try
        {
            var actions = new[] { "Execute", "Download", "Delete", "Rename" };
            var choice = await _dialogService.DisplayActionSheetAsync(file.Name, "Cancel", null, actions);

            switch (choice)
            {
                case "Execute":
                    await ExecuteRemoteFileAsync(file);
                    break;
                case "Download":
                    await DownloadRemoteFileAsync(file);
                    break;
                case "Delete":
                    await DeleteRemoteFileAsync(file);
                    break;
                case "Rename":
                    await RenameRemoteFileAsync(file);
                    break;
            }
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"File action failed: {ex.Message}\n");
        }
    }

    private async Task ShowLocalFileActionsAsync(FileSystemInfo file)
    {
        try
        {
            var actions = new[] { "Upload", "Delete", "Rename" };
            var choice = await _dialogService.DisplayActionSheetAsync(file.Name, "Cancel", null, actions);

            switch (choice)
            {
                case "Upload":
                    if (file is FileInfo fi)
                        await UploadLocalFileAsync(fi);
                    break;
                case "Delete":
                    await DeleteLocalFileAsync(file);
                    break;
                case "Rename":
                    await RenameLocalFileAsync(file);
                    break;
            }
        }
        catch (Exception ex)
        {
            await _terminalService.AppendOutputAsync($"File action failed: {ex.Message}\n");
        }
    }

    private async Task ExecuteRemoteFileAsync(SftpFile file)
    {
        var ok = await _dialogService.DisplayAlertAsync("Execute", $"Execute {file.Name}?", "OK", "Cancel");
        if (!ok) return;

        var remotePath = PathHelpers.CombineUnix(SshState.RemotePath, file.Name);
        var command = $"chmod +x '{remotePath}' && '{remotePath}'";
        await _terminalService.AppendOutputAsync($"> {command}\n");
        
        var result = await _sshService.ExecuteCommandAsync(command);
        await _terminalService.AppendOutputAsync(result + "\n");
    }

    private async Task DownloadRemoteFileAsync(SftpFile file)
    {
        var remotePath = PathHelpers.CombineUnix(SshState.RemotePath, file.Name);
        var localPath = Path.Combine(SshState.LocalPath, file.Name);
        
        await _sshService.DownloadFileAsync(remotePath, localPath);
        await _terminalService.AppendOutputAsync($"Downloaded {file.Name}\n");
        await RefreshLocalAsync();
    }

    private async Task UploadLocalFileAsync(FileInfo file)
    {
        var localPath = file.FullName;
        var remotePath = PathHelpers.CombineUnix(SshState.RemotePath, file.Name);
        
        await _sshService.UploadFileAsync(localPath, remotePath);
        await _terminalService.AppendOutputAsync($"Uploaded {file.Name}\n");
        await RefreshRemoteAsync();
    }

    private async Task DeleteRemoteFileAsync(SftpFile file)
    {
        var ok = await _dialogService.DisplayAlertAsync("Delete", $"Delete {file.Name}?", "OK", "Cancel");
        if (!ok) return;

        var remotePath = PathHelpers.CombineUnix(SshState.RemotePath, file.Name);
        var command = file.IsDirectory ? $"rm -rf '{remotePath}'" : $"rm -f '{remotePath}'";
        
        await _sshService.ExecuteCommandAsync(command);
        await _terminalService.AppendOutputAsync($"Deleted {file.Name}\n");
        await RefreshRemoteAsync();
    }

    private async Task DeleteLocalFileAsync(FileSystemInfo file)
    {
        var ok = await _dialogService.DisplayAlertAsync("Delete", $"Delete {file.Name}?", "OK", "Cancel");
        if (!ok) return;

        if (file is DirectoryInfo dir)
            dir.Delete(true);
        else
            File.Delete(file.FullName);

        await _terminalService.AppendOutputAsync($"Deleted {file.Name}\n");
        await RefreshLocalAsync();
    }

    private async Task RenameRemoteFileAsync(SftpFile file)
    {
        var newName = await _dialogService.DisplayPromptAsync("Rename", "New name:", initialValue: file.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == file.Name) return;

        var oldPath = PathHelpers.CombineUnix(SshState.RemotePath, file.Name);
        var newPath = PathHelpers.CombineUnix(SshState.RemotePath, newName);
        var command = $"mv '{oldPath}' '{newPath}'";

        await _sshService.ExecuteCommandAsync(command);
        await _terminalService.AppendOutputAsync($"Renamed {file.Name} to {newName}\n");
        await RefreshRemoteAsync();
    }

    private async Task RenameLocalFileAsync(FileSystemInfo file)
    {
        var newName = await _dialogService.DisplayPromptAsync("Rename", "New name:", initialValue: file.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == file.Name) return;

        var newPath = Path.Combine(Path.GetDirectoryName(file.FullName)!, newName);
        
        if (file is DirectoryInfo)
            Directory.Move(file.FullName, newPath);
        else
            File.Move(file.FullName, newPath, true);

        await _terminalService.AppendOutputAsync($"Renamed {file.Name} to {newName}\n");
        await RefreshLocalAsync();
    }

    // UI Interaction Methods
    private async Task StartRemoteDragAsync(SftpFile file)
    {
        var remotePath = PathHelpers.CombineUnix(SshState.RemotePath, file.Name);
        await _uiInteractionService.StartDragAsync(remotePath, file.Name, isRemoteSource: true);
    }

    private async Task StartLocalDragAsync(FileSystemInfo file)
    {
        await _uiInteractionService.StartDragAsync(file.FullName, file.Name, isRemoteSource: false);
    }

    private async Task DropOnRemoteAsync()
    {
        await _uiInteractionService.HandleDropAsync(isRemoteTarget: true);
    }

    private async Task DropOnLocalAsync()
    {
        await _uiInteractionService.HandleDropAsync(isRemoteTarget: false);
    }

    private async Task StartTerminalResizeAsync(double currentHeight)
    {
        await _uiInteractionService.StartTerminalResizeAsync(currentHeight);
    }

    private async Task UpdateTerminalHeightAsync(double deltaY)
    {
        await _uiInteractionService.UpdateTerminalHeightAsync(deltaY);
    }

    private async Task EndTerminalResizeAsync()
    {
        await _uiInteractionService.EndTerminalResizeAsync();
    }

    private async Task ShowCommandsAsync()
    {
        var command = await _uiInteractionService.ShowCommandsActionSheetAsync();
        if (!string.IsNullOrEmpty(command))
        {
            await ExecuteCommandAsync(command);
        }
    }
}
