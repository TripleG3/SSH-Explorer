using System.Collections.ObjectModel;

namespace SSHExplorer.Models;

public readonly record struct SessionState(
    bool IsBusy,
    ObservableCollection<Session> Sessions,
    Session? ActiveSession,
    string ErrorMessage)
{
    public static readonly SessionState Empty = new(
        false,
        new ObservableCollection<Session>(),
        null,
        string.Empty);
}