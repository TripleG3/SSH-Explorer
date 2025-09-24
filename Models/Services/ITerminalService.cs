using SSHExplorer.Models;

namespace SSHExplorer.Models.Services;

public interface ITerminalService : IStatePublisher<TerminalState>
{
    Task SetInputAsync(string input, CancellationToken ct = default);
    Task AppendOutputAsync(string output, CancellationToken ct = default);
    Task ClearOutputAsync(CancellationToken ct = default);
    Task ToggleVisibilityAsync(CancellationToken ct = default);
    Task TogglePinAsync(CancellationToken ct = default);
    Task SetHeightAsync(double height, CancellationToken ct = default);
    Task SetPaneSplitRatioAsync(double ratio, CancellationToken ct = default);
    Task ExecuteLocalCommandAsync(string command, CancellationToken ct = default);
    Task ExecuteRemoteCommandAsync(string command, CancellationToken ct = default);
}
