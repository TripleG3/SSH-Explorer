namespace SSHExplorer.Models;

public readonly record struct TerminalState(
    bool IsBusy,
    string Output,
    string Input,
    bool IsVisible,
    bool IsPinned,
    double Height,
    double PaneSplitRatio,
    string ErrorMessage)
{
    public static readonly TerminalState Empty = new(
        false,
        string.Empty,
        string.Empty,
        true,
        false,
        220.0,
        0.5,
        string.Empty);
}