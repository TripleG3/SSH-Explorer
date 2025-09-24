namespace SSHExplorer.Models;

/// <summary>
/// Represents the state of drag and drop operations
/// </summary>
public readonly record struct DragDropState(
    bool IsDragging,
    string? DraggedItemPath,
    string? DraggedItemName,
    bool IsRemoteSource,
    string ErrorMessage)
{
    public static readonly DragDropState Empty = new(false, null, null, false, string.Empty);
}

/// <summary>
/// Represents the state of terminal resize operations
/// </summary>
public readonly record struct TerminalResizeState(
    bool IsResizing,
    double StartHeight,
    double CurrentHeight,
    double MinHeight,
    double MaxHeight)
{
    public static readonly TerminalResizeState Empty = new(false, 0, 300, 120, 600);
    
    public TerminalResizeState WithCurrentHeight(double height) =>
        this with { CurrentHeight = Math.Max(MinHeight, Math.Min(MaxHeight, height)) };
}

/// <summary>
/// Represents the state of pin icon display
/// </summary>
public readonly record struct PinIconState(
    bool IsPinned,
    string? PinnedIconPath,
    string? UnpinnedIconPath)
{
    public static readonly PinIconState Empty = new(false, null, null);
}

/// <summary>
/// Represents the state of keyboard accelerators
/// </summary>
public readonly record struct KeyboardAcceleratorState(
    bool IsTerminalToggleEnabled,
    string TerminalToggleKey,
    bool ArePlatformAcceleratorsRegistered)
{
    public static readonly KeyboardAcceleratorState Default = new(true, "Ctrl+Shift+T", false);
}

/// <summary>
/// Combined UI interaction state following CIS pattern
/// </summary>
public readonly record struct UiInteractionState(
    DragDropState DragDrop,
    TerminalResizeState TerminalResize,
    PinIconState PinIcon,
    KeyboardAcceleratorState KeyboardAccelerator,
    bool IsBusy,
    string ErrorMessage)
{
    public static readonly UiInteractionState Empty = new(
        DragDropState.Empty,
        TerminalResizeState.Empty,
        PinIconState.Empty,
        KeyboardAcceleratorState.Default,
        false,
        string.Empty);
}