using System.Collections.ObjectModel;

namespace SSHExplorer.Models.Services;

public sealed class SessionService : StatePublisher<SessionState>, ISessionService
{
    private readonly ISshService _sshService;
    private readonly Dictionary<string, ISshService> _sessionSshServices = new();

    public SessionService(ISshService sshService) : base(SessionState.Empty)
    {
        _sshService = sshService;
    }

    public async Task CreateLocalSessionAsync(string name, string? initialPath = null, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });

        try
        {
            var session = await Task.Run(() => Session.CreateLocal(name, initialPath ?? string.Empty), ct);
            var sessions = new ObservableCollection<Session>(State.Sessions) { session };

            SetState(State with 
            { 
                IsBusy = false,
                Sessions = sessions,
                ActiveSession = session,
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task CreateSshSessionAsync(Profile profile, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });

        try
        {
            var session = await Task.Run(() => Session.CreateSsh(profile), ct);
            var sessions = new ObservableCollection<Session>(State.Sessions) { session };

            // Create a dedicated SSH service for this session
            // Note: In a real implementation, you might want to inject a factory for SSH services
            // For now, we'll track this session for connection management
            
            SetState(State with 
            { 
                IsBusy = false,
                Sessions = sessions,
                ActiveSession = session,
                ErrorMessage = string.Empty 
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task CloseSessionAsync(string sessionId, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });

        try
        {
            await Task.Run(() =>
            {
                var session = State.Sessions.FirstOrDefault(s => s.Id == sessionId);
                if (session.Equals(default(Session)))
                {
                    SetState(State with { IsBusy = false, ErrorMessage = "Session not found" });
                    return;
                }

                // Cleanup SSH connection if it's an SSH session
                if (session.Type == SessionType.Ssh && _sessionSshServices.ContainsKey(sessionId))
                {
                    // Disconnect and cleanup
                    _sessionSshServices.Remove(sessionId);
                }

                var sessions = new ObservableCollection<Session>(State.Sessions.Where(s => s.Id != sessionId));
                var activeSession = State.ActiveSession?.Id == sessionId 
                    ? sessions.FirstOrDefault() 
                    : State.ActiveSession;

                SetState(State with 
                { 
                    IsBusy = false,
                    Sessions = sessions,
                    ActiveSession = activeSession,
                    ErrorMessage = string.Empty 
                });
            }, ct);
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task SetActiveSessionAsync(string sessionId, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var session = State.Sessions.FirstOrDefault(s => s.Id == sessionId);
            if (!session.Equals(default(Session)))
            {
                SetState(State with { ActiveSession = session });
            }
        }, ct);
    }

    public async Task UpdateSessionPathAsync(string sessionId, string path, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var sessions = new ObservableCollection<Session>();
            var activeSession = State.ActiveSession;

            foreach (var session in State.Sessions)
            {
                if (session.Id == sessionId)
                {
                    var updatedSession = session with { CurrentPath = path };
                    sessions.Add(updatedSession);
                    
                    if (State.ActiveSession?.Id == sessionId)
                    {
                        activeSession = updatedSession;
                    }
                }
                else
                {
                    sessions.Add(session);
                }
            }

            SetState(State with { Sessions = sessions, ActiveSession = activeSession });
        }, ct);
    }

    public async Task UpdateSessionConnectionStatusAsync(string sessionId, bool isConnected, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var sessions = new ObservableCollection<Session>();
            var activeSession = State.ActiveSession;

            foreach (var session in State.Sessions)
            {
                if (session.Id == sessionId)
                {
                    var updatedSession = session with { IsConnected = isConnected };
                    sessions.Add(updatedSession);
                    
                    if (State.ActiveSession?.Id == sessionId)
                    {
                        activeSession = updatedSession;
                    }
                }
                else
                {
                    sessions.Add(session);
                }
            }

            SetState(State with { Sessions = sessions, ActiveSession = activeSession });
        }, ct);
    }

    public Session? GetSession(string sessionId)
    {
        var session = State.Sessions.FirstOrDefault(s => s.Id == sessionId);
        return session.Equals(default(Session)) ? null : session;
    }
}