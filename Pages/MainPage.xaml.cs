using SSHExplorer.ViewModels;

namespace SSHExplorer.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Appearing += (_, _) => vm.LoadCommand.Execute(null);
    }
}
