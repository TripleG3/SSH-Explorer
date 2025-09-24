using SSHExplorer.ViewModels;

namespace SSHExplorer.Views;

public partial class SshConnectionToolbarView : ContentView
{
    public SshConnectionToolbarView(SshConnectionToolbarViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}