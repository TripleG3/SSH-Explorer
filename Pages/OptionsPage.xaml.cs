using SSHExplorer.ViewModels;

namespace SSHExplorer.Pages;

public partial class OptionsPage : ContentPage
{
    public OptionsPage(OptionsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}