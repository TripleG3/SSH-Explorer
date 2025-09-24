namespace SSHExplorer.Services;

public sealed class ThemeService : IThemeService
{
    private const string THEME_PREFERENCE_KEY = "AppTheme";
    
    public event EventHandler<AppTheme>? ThemeChanged;

    public async Task InitializeAsync()
    {
        // Load saved theme preference
        var savedTheme = Preferences.Get(THEME_PREFERENCE_KEY, "System");
        var theme = savedTheme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified // System default
        };

        await SetThemeAsync(theme);
    }

    public AppTheme GetCurrentTheme()
    {
        if (Application.Current?.UserAppTheme == null)
            return AppTheme.Unspecified;
        
        return Application.Current.UserAppTheme;
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = theme;
            }
        });

        // Save preference
        var themeString = theme switch
        {
            AppTheme.Light => "Light",
            AppTheme.Dark => "Dark",
            _ => "System"
        };
        Preferences.Set(THEME_PREFERENCE_KEY, themeString);

        // Notify listeners
        ThemeChanged?.Invoke(this, theme);
    }
}
