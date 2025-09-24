using Microsoft.Maui.ApplicationModel;
using Renci.SshNet.Common;
using SSHExplorer.Models.Services;

namespace SSHExplorer;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Global safety net: convert unhandled exceptions into friendly popups
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	private static int _showingError;
	private static void ShowFriendlyError(string title, string message)
	{
		if (Interlocked.Exchange(ref _showingError, 1) == 1)
			return; // avoid cascades
		try
		{
			_ = MainThread.InvokeOnMainThreadAsync(async () =>
			{
				try
				{
					var dialogs = new DialogService();
					await dialogs.DisplayMessageAsync(title, message);
				}
				finally { Interlocked.Exchange(ref _showingError, 0); }
			});
		}
		catch
		{
			Interlocked.Exchange(ref _showingError, 0);
		}
	}

	private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var ex = e.ExceptionObject as Exception;
		if (ex is null) return;
		var (title, msg) = ClassifyException(ex);
		ShowFriendlyError(title, msg);
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		var (title, msg) = ClassifyException(e.Exception);
		ShowFriendlyError(title, msg);
		e.SetObserved();
	}

	private static (string Title, string Message) ClassifyException(Exception ex)
	{
		// Prefer inner-most cause message
		Exception root = ex;
		while (root.InnerException is not null) root = root.InnerException;

		if (root is SftpPermissionDeniedException)
			return ("Permission Denied", "You don't have permission to access this folder or file on the server.");

		return ("Operation Failed", root.Message);
	}
}