using SSHExplorer.Models;

namespace SSHExplorer.Models.Services;

/// <summary>
/// Service for handling UI-specific interactions like drag/drop, terminal resize, and keyboard shortcuts
/// </summary>
public interface IUiInteractionService : IStatePublisher<UiInteractionState>
{
    /// <summary>
    /// Start drag operation with the specified item
    /// </summary>
    Task StartDragAsync(string itemPath, string itemName, bool isRemoteSource);

    /// <summary>
    /// Handle drop operation with the dragged item
    /// </summary>
    Task HandleDropAsync(bool isRemoteTarget);

    /// <summary>
    /// Clear current drag operation
    /// </summary>
    Task ClearDragAsync();

    /// <summary>
    /// Start terminal resize operation
    /// </summary>
    Task StartTerminalResizeAsync(double currentHeight);

    /// <summary>
    /// Update terminal height during resize
    /// </summary>
    Task UpdateTerminalHeightAsync(double deltaY);

    /// <summary>
    /// End terminal resize operation
    /// </summary>
    Task EndTerminalResizeAsync();

    /// <summary>
    /// Update pin icon paths and current state
    /// </summary>
    Task UpdatePinIconAsync(bool isPinned, string? pinnedIconPath = null, string? unpinnedIconPath = null);

    /// <summary>
    /// Register platform-specific keyboard accelerators
    /// </summary>
    Task RegisterKeyboardAcceleratorsAsync();

    /// <summary>
    /// Show about dialog with company and version information
    /// </summary>
    Task ShowAboutDialogAsync();

    /// <summary>
    /// Navigate to Options page
    /// </summary>
    Task NavigateToOptionsAsync();

    /// <summary>
    /// Show commands action sheet and return selected command
    /// </summary>
    Task<string?> ShowCommandsActionSheetAsync();
}
