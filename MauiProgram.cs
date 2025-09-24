using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SSHExplorer.Services;
using SSHExplorer.ViewModels;
using SSHExplorer.Views;

namespace SSHExplorer;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Core Services using CIS pattern
		builder.Services.AddSingleton<ISshService, SshService>();
		builder.Services.AddSingleton<IProfileService, ProfileService>();
		builder.Services.AddSingleton<IFileExplorerService, FileExplorerService>();
		builder.Services.AddSingleton<ITerminalService, TerminalService>();
		
		// UI Services
		builder.Services.AddSingleton<IThemeService, ThemeService>();
		builder.Services.AddSingleton<IDialogService, DialogService>();
		builder.Services.AddSingleton<IUiInteractionService, UiInteractionService>();
		
		// ViewModels & Views
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddTransient<OptionsViewModel>();
		builder.Services.AddTransient<OptionsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
