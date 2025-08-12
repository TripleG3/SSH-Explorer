namespace SSHExplorer.Services;

public interface IThemeService
{
    void ApplyLightTheme(Color primary);
    void ApplyDarkTheme(Color primary);
}
