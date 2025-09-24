using System.ComponentModel;
using System.Windows.Input;
using SSHExplorer.Models.Services;

namespace SSHExplorer.ViewModels;

public sealed class OptionsViewModel : INotifyPropertyChanged
{
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;

    private bool _isSystemTheme;
    private bool _isLightTheme;
    private bool _isDarkTheme;
    private double _terminalHeight = 300;
    private bool _startTerminalPinned;
    private bool _autoConnectLastProfile;

    public OptionsViewModel(IThemeService themeService, IDialogService dialogService)
    {
        _themeService = themeService;
        _dialogService = dialogService;

        SaveCommand = new AsyncBindingCommand(_ => SaveAsync(), _ => true, this);

        LoadSettings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // Theme properties
    public bool IsSystemTheme
    {
        get => _isSystemTheme;
        set
        {
            if (_isSystemTheme != value)
            {
                _isSystemTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSystemTheme)));
                if (value)
                {
                    IsLightTheme = false;
                    IsDarkTheme = false;
                }
            }
        }
    }

    public bool IsLightTheme
    {
        get => _isLightTheme;
        set
        {
            if (_isLightTheme != value)
            {
                _isLightTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLightTheme)));
                if (value)
                {
                    IsSystemTheme = false;
                    IsDarkTheme = false;
                }
            }
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDarkTheme)));
                if (value)
                {
                    IsSystemTheme = false;
                    IsLightTheme = false;
                }
            }
        }
    }

    // Terminal properties
    public double TerminalHeight
    {
        get => _terminalHeight;
        set
        {
            if (Math.Abs(_terminalHeight - value) > 0.1)
            {
                _terminalHeight = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TerminalHeight)));
            }
        }
    }

    public bool StartTerminalPinned
    {
        get => _startTerminalPinned;
        set
        {
            if (_startTerminalPinned != value)
            {
                _startTerminalPinned = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartTerminalPinned)));
            }
        }
    }

    // Connection properties
    public bool AutoConnectLastProfile
    {
        get => _autoConnectLastProfile;
        set
        {
            if (_autoConnectLastProfile != value)
            {
                _autoConnectLastProfile = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoConnectLastProfile)));
            }
        }
    }

    public ICommand SaveCommand { get; }

    private void LoadSettings()
    {
        // Load theme setting
        var currentTheme = _themeService.GetCurrentTheme();
        switch (currentTheme)
        {
            case AppTheme.Unspecified:
                IsSystemTheme = true;
                break;
            case AppTheme.Light:
                IsLightTheme = true;
                break;
            case AppTheme.Dark:
                IsDarkTheme = true;
                break;
        }

        // Load other settings from preferences
        TerminalHeight = Preferences.Get("TerminalHeight", 300.0);
        StartTerminalPinned = Preferences.Get("StartTerminalPinned", false);
        AutoConnectLastProfile = Preferences.Get("AutoConnectLastProfile", false);
    }

    private async Task SaveAsync()
    {
        try
        {
            // Save theme setting
            AppTheme selectedTheme = AppTheme.Unspecified;
            if (IsLightTheme) selectedTheme = AppTheme.Light;
            else if (IsDarkTheme) selectedTheme = AppTheme.Dark;

            await _themeService.SetThemeAsync(selectedTheme);

            // Save other preferences
            Preferences.Set("TerminalHeight", TerminalHeight);
            Preferences.Set("StartTerminalPinned", StartTerminalPinned);
            Preferences.Set("AutoConnectLastProfile", AutoConnectLastProfile);

            // Show success message with a small delay to prevent UI race conditions
            await Task.Delay(100);
            await _dialogService.DisplayMessageAsync("Settings Saved", "Your preferences have been saved successfully.");
            
            // Navigate back to avoid UI disposal issues
            await Task.Delay(100);
            await Shell.Current.GoToAsync("..");
        }
        catch (ObjectDisposedException)
        {
            // Ignore disposal exceptions as they happen during UI cleanup
            // The settings were likely saved successfully
        }
        catch (Exception ex)
        {
            try
            {
                await _dialogService.DisplayMessageAsync("Error", $"Failed to save settings: {ex.Message}");
            }
            catch
            {
                // If even the error dialog fails, we can't do much more
            }
        }
    }
}