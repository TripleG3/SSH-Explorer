using Specky7;
using System;
using TripleG3.SSH.WinUI.Models.Profiles;

namespace TripleG3.SSH.WinUI.ViewModels.Profiles;

[Singleton]
public sealed partial class ProfileServiceViewModel : ViewModelBase, IDisposable
{
    private readonly IProfileService _service;

    private bool _isBusy;
    private string _name = string.Empty;
    private string _address = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private int _port;

    public ProfileServiceViewModel(IProfileService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        // Initialize from current service state
        ApplyState(_service.State);
        _service.StateChanged += OnServiceStateChanged;

        CreateProfileCommand = new Commands.BindingCommand(CreateProfile, CanCreateProfile, this);
        UpdateProfileCommand = new Commands.BindingCommand(UpdateProfile, CanUpdateProfile, this);
        LoadProfileCommand = new Commands.BindingCommand<string>(LoadProfile, CanLoadOrDeleteProfile, this);
        DeleteProfileCommand = new Commands.BindingCommand<string>(DeleteProfile, CanLoadOrDeleteProfile, this);
    }

    // Bindable state (CIS: State)
    public bool IsBusy
    {
        get => _isBusy;
        private set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
    }

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public string Address
    {
        get => _address;
        set { if (_address != value) { _address = value; OnPropertyChanged(); } }
    }

    public string Username
    {
        get => _username;
        set { if (_username != value) { _username = value; OnPropertyChanged(); } }
    }

    public string Password
    {
        get => _password;
        set { if (_password != value) { _password = value; OnPropertyChanged(); } }
    }

    public int Port
    {
        get => _port;
        set { if (_port != value) { _port = value; OnPropertyChanged(); } }
    }

    // Commands (CIS: Commands/Intents)
    public Commands.BindingCommand CreateProfileCommand { get; }
    public Commands.BindingCommand UpdateProfileCommand { get; }
    public Commands.BindingCommand<string> LoadProfileCommand { get; }
    public Commands.BindingCommand<string> DeleteProfileCommand { get; }

    private void OnServiceStateChanged(ProfileState state) => ApplyState(state);

    private void ApplyState(ProfileState state)
    {
        IsBusy = state.IsBusy;
        // When profile changes, reflect into editable fields
        var p = state.Profile;
        if (!string.Equals(Name, p.Name, StringComparison.Ordinal)) Name = p.Name;
        if (!string.Equals(Address, p.Address, StringComparison.Ordinal)) Address = p.Address;
        if (!string.Equals(Username, p.Username, StringComparison.Ordinal)) Username = p.Username;
        if (!string.Equals(Password, p.Password, StringComparison.Ordinal)) Password = p.Password;
        if (Port != p.Port) Port = p.Port;
    }

    // Command impls
    private bool CanCreateProfile() => !IsBusy && !string.IsNullOrWhiteSpace(Name);
    private async void CreateProfile()
    {
        try
        {
            await _service.CreateProfile(new Profile(Name, Address, Username, Password, Port));
        }
        catch
        {
            // Intentionally swallow here; UI layer may handle via global error handler.
        }
    }

    private bool CanUpdateProfile() => !IsBusy && !string.IsNullOrWhiteSpace(Name);
    private async void UpdateProfile()
    {
        try
        {
            await _service.UpdateProfile(new Profile(Name, Address, Username, Password, Port));
        }
        catch
        {
        }
    }

    private bool CanLoadOrDeleteProfile(string? name) => !IsBusy && !string.IsNullOrWhiteSpace(name);

    private async void LoadProfile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            await _service.LoadProfile(name);
        }
        catch
        {
        }
    }

    private async void DeleteProfile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            await _service.DeleteProfile(name);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _service.StateChanged -= OnServiceStateChanged;
    }
}
