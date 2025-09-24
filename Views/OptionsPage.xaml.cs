using SSHExplorer.ViewModels;

namespace SSHExplorer.Views;

public partial class OptionsPage : ContentPage
{
	public OptionsPage(OptionsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}