using System.Collections.ObjectModel;

namespace SSHExplorer.Models;

public readonly record struct ProfileState(
    bool IsBusy,
    ObservableCollection<Profile> Profiles,
    Profile? SelectedProfile,
    string ErrorMessage)
{
    public static readonly ProfileState Empty = new(
        false,
        new ObservableCollection<Profile>(),
        null,
        string.Empty);
}