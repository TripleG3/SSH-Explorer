using SSHExplorer.Views;

namespace SSHExplorer;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(OptionsPage), typeof(OptionsPage));
	}
}
