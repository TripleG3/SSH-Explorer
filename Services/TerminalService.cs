using SSHExplorer.Models;

namespace SSHExplorer.Services;

public sealed class TerminalService : StatePublisher<TerminalState>, ITerminalService
{
    public TerminalService() : base(TerminalState.Empty)
    {
        // Restore UI state from preferences
        var initialState = State with
        {
            IsVisible = Preferences.Get("IsTerminalVisible", true),
            IsPinned = Preferences.Get("IsTerminalPinned", false),
            Height = Preferences.Get("TerminalHeight", 220.0),
            PaneSplitRatio = Math.Clamp(Preferences.Get("PaneSplitRatio", 0.5), 0.1, 0.9)
        };
        
        SetState(initialState);
    }

    public async Task SetInputAsync(string input, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            SetState(State with { Input = input });
        }, ct);
    }

    public async Task AppendOutputAsync(string output, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            SetState(State with { Output = State.Output + output });
        }, ct);
    }

    public async Task ClearOutputAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            SetState(State with { Output = string.Empty });
        }, ct);
    }

    public async Task ToggleVisibilityAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var newVisibility = !State.IsVisible;
            SetState(State with { IsVisible = newVisibility });
            Preferences.Set("IsTerminalVisible", newVisibility);
        }, ct);
    }

    public async Task TogglePinAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var newPinned = !State.IsPinned;
            SetState(State with { IsPinned = newPinned });
            Preferences.Set("IsTerminalPinned", newPinned);
        }, ct);
    }

    public async Task SetHeightAsync(double height, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            SetState(State with { Height = height });
            Preferences.Set("TerminalHeight", height);
        }, ct);
    }

    public async Task SetPaneSplitRatioAsync(double ratio, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var clampedRatio = Math.Clamp(ratio, 0.1, 0.9);
            SetState(State with { PaneSplitRatio = clampedRatio });
            Preferences.Set("PaneSplitRatio", clampedRatio);
        }, ct);
    }
}