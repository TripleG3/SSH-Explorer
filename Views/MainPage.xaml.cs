using SSHExplorer.ViewModels;
using System.IO;
using System.Linq;

namespace SSHExplorer.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Appearing += (_, _) => vm.LoadCommand.Execute(null);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            // Initialize UI state and register keyboard accelerators
            vm.RegisterKeyboardAcceleratorsCommand.Execute(null);
        }
    }

    // Drag/Drop event handlers - delegate to ViewModel commands
    private void OnRemoteDragStarting(object sender, DragStartingEventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is Renci.SshNet.Sftp.SftpFile sftpFile
            && BindingContext is MainViewModel vm)
        {
            vm.StartRemoteDragCommand.Execute(sftpFile);
            e.Data.Properties["remoteName"] = sftpFile.Name;
        }
    }

    private void OnLocalDrop(object sender, DropEventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            vm.DropOnLocalCommand.Execute(null);
        }
    }

    private void OnLocalDragStarting(object sender, DragStartingEventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is FileSystemInfo fsi
            && BindingContext is MainViewModel vm)
        {
            vm.StartLocalDragCommand.Execute(fsi);
            e.Data.Properties["localPath"] = fsi.FullName;
        }
    }

    private void OnRemoteDrop(object sender, DropEventArgs e)
    {
        if (BindingContext is MainViewModel vm)
        {
            vm.DropOnRemoteCommand.Execute(null);
        }
    }

    // Terminal resize handler - delegate to ViewModel commands
    private void OnTerminalHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (BindingContext is not MainViewModel vm) return;
        
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                vm.StartTerminalResizeCommand.Execute(vm.TerminalState.Height);
                break;
            case GestureStatus.Running:
                vm.UpdateTerminalHeightCommand.Execute(e.TotalY);
                // Update UI immediately for smooth resize
                TerminalContainer.HeightRequest = vm.UiInteractionState.TerminalResize.CurrentHeight;
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                vm.EndTerminalResizeCommand.Execute(null);
                break;
        }
    }

#if WINDOWS
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        if (BindingContext is not MainViewModel vm) return;
        
        try
        {
            var page = this.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Page;
            if (page is null) return;
            
            // Add Ctrl+Shift+T accelerator if not already present
            bool exists = page.KeyboardAccelerators.Any(k => k.Key == Windows.System.VirtualKey.T);
            if (!exists)
            {
                var accel = new Microsoft.UI.Xaml.Input.KeyboardAccelerator
                {
                    Key = Windows.System.VirtualKey.T,
                    Modifiers = Windows.System.VirtualKeyModifiers.Control | Windows.System.VirtualKeyModifiers.Shift
                };
                accel.Invoked += (s, e) =>
                {
                    vm.ToggleTerminalCommand.Execute(null);
                    e.Handled = true;
                };
                page.KeyboardAccelerators.Add(accel);
            }
        }
        catch { } // Ignore platform-specific errors
    }
#endif
}
