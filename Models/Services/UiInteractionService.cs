using SSHExplorer.Models;
using Microsoft.Maui.ApplicationModel;

namespace SSHExplorer.Models.Services;

/// <summary>
/// Service for handling UI-specific interactions following CIS pattern
/// </summary>
public sealed class UiInteractionService : StatePublisher<UiInteractionState>, IUiInteractionService
{
    private readonly IDialogService _dialogService;
    private readonly ISshService _sshService;
    private readonly IFileExplorerService _fileExplorerService;
    private readonly ITerminalService _terminalService;

    public UiInteractionService(
        IDialogService dialogService,
        ISshService sshService,
        IFileExplorerService fileExplorerService,
        ITerminalService terminalService) 
        : base(UiInteractionState.Empty)
    {
        _dialogService = dialogService;
        _sshService = sshService;
        _fileExplorerService = fileExplorerService;
        _terminalService = terminalService;
    }

    public async Task StartDragAsync(string itemPath, string itemName, bool isRemoteSource)
    {
        SetState(State with 
        {
            DragDrop = State.DragDrop with 
            {
                IsDragging = true,
                DraggedItemPath = itemPath,
                DraggedItemName = itemName,
                IsRemoteSource = isRemoteSource,
                ErrorMessage = string.Empty
            }
        });
        
        await Task.CompletedTask;
    }

    public async Task HandleDropAsync(bool isRemoteTarget)
    {
        if (!State.DragDrop.IsDragging || string.IsNullOrEmpty(State.DragDrop.DraggedItemPath))
        {
            await ClearDragAsync();
            return;
        }

        try
        {
            SetState(State with { IsBusy = true });

            // Remote to Local (Download)
            if (State.DragDrop.IsRemoteSource && !isRemoteTarget)
            {
                var remotePath = State.DragDrop.DraggedItemPath;
                var localPath = Path.Combine(_sshService.State.LocalPath, State.DragDrop.DraggedItemName!);
                
                await _sshService.DownloadFileAsync(remotePath, localPath);
                await _terminalService.AppendOutputAsync($"Downloaded {State.DragDrop.DraggedItemName}\n");
                await _fileExplorerService.RefreshLocalAsync(_sshService.State.LocalPath);
            }
            // Local to Remote (Upload)
            else if (!State.DragDrop.IsRemoteSource && isRemoteTarget)
            {
                var localPath = State.DragDrop.DraggedItemPath;
                var remotePath = $"{_sshService.State.RemotePath}/{State.DragDrop.DraggedItemName}";
                
                await _sshService.UploadFileAsync(localPath, remotePath);
                await _terminalService.AppendOutputAsync($"Uploaded {State.DragDrop.DraggedItemName}\n");
                await _fileExplorerService.RefreshRemoteAsync(_sshService.State.RemotePath);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"File transfer failed: {ex.Message}";
            SetState(State with 
            {
                DragDrop = State.DragDrop with { ErrorMessage = errorMsg },
                ErrorMessage = errorMsg
            });
            await _terminalService.AppendOutputAsync($"{errorMsg}\n");
            await _dialogService.DisplayMessageAsync("Transfer Error", errorMsg);
        }
        finally
        {
            SetState(State with { IsBusy = false });
            await ClearDragAsync();
        }
    }

    public async Task ClearDragAsync()
    {
        SetState(State with 
        {
            DragDrop = DragDropState.Empty
        });
        
        await Task.CompletedTask;
    }

    public async Task StartTerminalResizeAsync(double currentHeight)
    {
        SetState(State with 
        {
            TerminalResize = State.TerminalResize with 
            {
                IsResizing = true,
                StartHeight = currentHeight,
                CurrentHeight = currentHeight
            }
        });
        
        await Task.CompletedTask;
    }

    public async Task UpdateTerminalHeightAsync(double deltaY)
    {
        if (!State.TerminalResize.IsResizing) return;

        var newHeight = State.TerminalResize.StartHeight + (-deltaY);
        var clampedHeight = Math.Max(State.TerminalResize.MinHeight, 
                                   Math.Min(State.TerminalResize.MaxHeight, newHeight));

        SetState(State with 
        {
            TerminalResize = State.TerminalResize.WithCurrentHeight(clampedHeight)
        });

        await Task.CompletedTask;
    }

    public async Task EndTerminalResizeAsync()
    {
        if (!State.TerminalResize.IsResizing) return;

        // Persist the terminal height
        await _terminalService.SetHeightAsync(State.TerminalResize.CurrentHeight);

        SetState(State with 
        {
            TerminalResize = State.TerminalResize with { IsResizing = false }
        });
    }

    public async Task UpdatePinIconAsync(bool isPinned, string? pinnedIconPath = null, string? unpinnedIconPath = null)
    {
        SetState(State with 
        {
            PinIcon = new PinIconState(
                isPinned,
                pinnedIconPath ?? State.PinIcon.PinnedIconPath,
                unpinnedIconPath ?? State.PinIcon.UnpinnedIconPath)
        });

        await Task.CompletedTask;
    }

    public async Task RegisterKeyboardAcceleratorsAsync()
    {
        try
        {
#if WINDOWS
            // Platform-specific keyboard accelerator registration will be handled
            // by the View through binding to this state
            SetState(State with 
            {
                KeyboardAccelerator = State.KeyboardAccelerator with 
                {
                    ArePlatformAcceleratorsRegistered = true
                }
            });
#endif
        }
        catch (Exception ex)
        {
            SetState(State with { ErrorMessage = $"Keyboard accelerator registration failed: {ex.Message}" });
        }

        await Task.CompletedTask;
    }

    public async Task ShowAboutDialogAsync()
    {
        try
        {
            var company = "Triple G3";
            var version = AppInfo.Current?.VersionString ?? "Unknown";
            await _dialogService.DisplayMessageAsync("About", $"Company: {company}\nVersion: {version}");
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayMessageAsync("Error", $"Could not show about dialog: {ex.Message}");
        }
    }

    public async Task NavigateToOptionsAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("OptionsPage");
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayMessageAsync("Navigation Error", $"Could not navigate to Options: {ex.Message}");
        }
    }

    public async Task<string?> ShowCommandsActionSheetAsync()
    {
        try
        {
            var choice = await _dialogService.DisplayActionSheetAsync("Popular Commands", "Cancel", null, "ls -la", "df -h", "top -b -n 1");
            if (string.IsNullOrEmpty(choice) || choice == "Cancel") return null;

            var ok = await _dialogService.DisplayAlertAsync("Confirm", choice, "OK", "Cancel");
            if (!ok) return null;

            return choice; // Return command for execution by MainViewModel
        }
        catch (Exception ex)
        {
            await _dialogService.DisplayMessageAsync("Error", $"Could not show commands: {ex.Message}");
            return null;
        }
    }
}
