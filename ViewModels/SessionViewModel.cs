using System.ComponentModel;
using System.Windows.Input;
using SSHExplorer.Models;
using SSHExplorer.Models.Services;

namespace SSHExplorer.ViewModels;

public sealed class SessionViewModel : INotifyPropertyChanged
{
    private readonly Session _session;
    private readonly IFileExplorerService _fileExplorerService;
    private readonly ITerminalService _terminalService;
    private readonly ITextEditorService _textEditorService;
    private readonly ISshService _sshService;
    private readonly ISessionService _sessionService;

    public SessionViewModel(
        Session session,
        IFileExplorerService fileExplorerService,
        ITerminalService terminalService,
        ITextEditorService textEditorService,
        ISshService sshService,
        ISessionService sessionService)
    {
        _session = session;
        _fileExplorerService = fileExplorerService;
        _terminalService = terminalService;
        _textEditorService = textEditorService;
        _sshService = sshService;
        _sessionService = sessionService;

        // Subscribe to state changes
        _fileExplorerService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileExplorerState)));
        _terminalService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TerminalState)));
        _textEditorService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextEditorState)));
        _sshService.StateChanged += _ => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SshState)));

        // Initialize commands
        ConnectCommand = new AsyncBindingCommand(async _ => await ConnectAsync(CancellationToken.None), _ => !IsConnected && !FileExplorerState.IsBusy, this);
        DisconnectCommand = new AsyncBindingCommand(async _ => await DisconnectAsync(CancellationToken.None), _ => IsConnected, this);
        RefreshCommand = new AsyncBindingCommand(async _ => await RefreshAsync(CancellationToken.None), _ => !FileExplorerState.IsBusy, this);
        NavigateUpCommand = new AsyncBindingCommand(async _ => await NavigateUpAsync(CancellationToken.None), _ => !FileExplorerState.IsBusy, this);
        ExecuteTerminalCommand = new AsyncBindingCommand(async _ => await ExecuteTerminalCommandAsync(CancellationToken.None), _ => !TerminalState.IsBusy, this);
        CloseSessionCommand = new AsyncBindingCommand(async _ => await CloseSessionAsync(CancellationToken.None), _ => true, this);
        
        // Initialize for local sessions or SSH sessions
        if (_session.Type == SessionType.Local)
        {
            _ = Task.Run(async () =>
            {
                await _sessionService.UpdateSessionConnectionStatusAsync(_session.Id, true, CancellationToken.None);
                await _fileExplorerService.NavigateToLocalAsync(_session.CurrentPath, CancellationToken.None);
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Session Session => _session;
    public FileExplorerState FileExplorerState => _fileExplorerService.State;
    public TerminalState TerminalState => _terminalService.State;
    public TextEditorState TextEditorState => _textEditorService.State;
    public SshConnectionState SshState => _sshService.State;

    public bool IsConnected => _session.Type == SessionType.Local || SshState.IsConnected;
    public bool IsLocal => _session.Type == SessionType.Local;
    public bool IsSsh => _session.Type == SessionType.Ssh;

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NavigateUpCommand { get; }
    public ICommand ExecuteTerminalCommand { get; }
    public ICommand CloseSessionCommand { get; }

    private async Task ConnectAsync(CancellationToken ct)
    {
        if (_session.Profile == null || _session.Type != SessionType.Ssh)
            return;

        try
        {
            await _sshService.ConnectAsync(_session.Profile.Value, ct);
            await _sessionService.UpdateSessionConnectionStatusAsync(_session.Id, true, ct);
            await _fileExplorerService.RefreshRemoteAsync(_session.CurrentPath, ct);
        }
        catch (Exception ex)
        {
            // Handle connection error
            await _sessionService.UpdateSessionConnectionStatusAsync(_session.Id, false, ct);
        }
    }

    private async Task DisconnectAsync(CancellationToken ct)
    {
        if (_session.Type == SessionType.Ssh)
        {
            await _sshService.DisconnectAsync(ct);
            await _sessionService.UpdateSessionConnectionStatusAsync(_session.Id, false, ct);
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        if (IsLocal)
        {
            await _fileExplorerService.RefreshLocalAsync(FileExplorerState.LocalPath, ct);
        }
        else if (IsConnected)
        {
            await _fileExplorerService.RefreshRemoteAsync(FileExplorerState.RemotePath, ct);
        }
    }

    private async Task NavigateUpAsync(CancellationToken ct)
    {
        if (IsLocal)
        {
            var currentPath = FileExplorerState.LocalPath;
            var parentPath = Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                await _fileExplorerService.NavigateToLocalAsync(parentPath, ct);
                await _sessionService.UpdateSessionPathAsync(_session.Id, parentPath, ct);
            }
        }
        else if (IsConnected)
        {
            var currentPath = FileExplorerState.RemotePath;
            var parentPath = currentPath.TrimEnd('/');
            var lastSlash = parentPath.LastIndexOf('/');
            if (lastSlash > 0)
            {
                parentPath = parentPath.Substring(0, lastSlash);
                await _fileExplorerService.NavigateToRemoteAsync(parentPath, ct);
                await _sessionService.UpdateSessionPathAsync(_session.Id, parentPath, ct);
            }
        }
    }

    private async Task ExecuteTerminalCommandAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(TerminalState.Input))
        {
            if (IsLocal)
            {
                await _terminalService.ExecuteLocalCommandAsync(TerminalState.Input, ct);
            }
            else if (IsConnected)
            {
                await _terminalService.ExecuteRemoteCommandAsync(TerminalState.Input, ct);
            }
        }
    }

    private async Task CloseSessionAsync(CancellationToken ct)
    {
        await _sessionService.CloseSessionAsync(_session.Id, ct);
    }
}