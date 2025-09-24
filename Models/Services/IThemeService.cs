namespace SSHExplorer.Models.Services;

public interface IThemeService
{
    AppTheme GetCurrentTheme();
    Task SetThemeAsync(AppTheme theme);
    Task InitializeAsync();
    event EventHandler<AppTheme>? ThemeChanged;
}
