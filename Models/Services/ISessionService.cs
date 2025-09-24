namespace SSHExplorer.Models.Services;

public interface ISessionService : IStatePublisher<SessionState>
{
    Task CreateLocalSessionAsync(string name, string? initialPath = null, CancellationToken ct = default);
    Task CreateSshSessionAsync(Profile profile, CancellationToken ct = default);
    Task CloseSessionAsync(string sessionId, CancellationToken ct = default);
    Task SetActiveSessionAsync(string sessionId, CancellationToken ct = default);
    Task UpdateSessionPathAsync(string sessionId, string path, CancellationToken ct = default);
    Task UpdateSessionConnectionStatusAsync(string sessionId, bool isConnected, CancellationToken ct = default);
    Session? GetSession(string sessionId);
}