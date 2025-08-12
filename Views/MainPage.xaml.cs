using SSHExplorer.ViewModels;

namespace SSHExplorer.Views;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Appearing += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
		this.Loaded += OnLoaded;
	}

	private async void OnCommandsClicked(object? sender, EventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		var choice = await DisplayActionSheet("Popular Commands", "Cancel", null, "ls -la", "df -h", "top -b -n 1");
		if (string.IsNullOrEmpty(choice) || choice == "Cancel") return;
		var ok = await DisplayAlert("Confirm", choice, "OK", "Cancel");
		if (!ok) return;
		var result = await vm.ExecuteCommandAsync(choice);
	}

	// Drag from remote item -> drop on local pane = download
	private void OnRemoteDragStarting(object sender, DragStartingEventArgs e)
	{
		if (sender is not BindableObject bo || bo.BindingContext is not Renci.SshNet.Sftp.SftpFile sftp) return;
		e.Data.Properties["remoteName"] = sftp.Name;
	}

	private async void OnLocalDrop(object sender, DropEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.Data.Properties.TryGetValue("remoteName", out var val) && val is string remoteName)
		{
			// trigger download of selected remote item
			var match = vm.RemoteItems.FirstOrDefault(r => r.Name == remoteName);
			if (match != null)
			{
				vm.SelectedRemoteItem = match;
				await vm.DownloadSelectedCommand.ExecuteAsync(null);
			}
		}
	}

	// Drag from local item -> drop on remote pane = upload
	private void OnLocalDragStarting(object sender, DragStartingEventArgs e)
	{
		if (sender is not BindableObject bo || bo.BindingContext is not FileSystemInfo fsi) return;
		e.Data.Properties["localPath"] = fsi.FullName;
	}

	private async void OnRemoteDrop(object sender, DropEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.Data.Properties.TryGetValue("localPath", out var val) && val is string localPath)
		{
			var fi = new FileInfo(localPath);
			if (fi.Exists)
			{
				vm.SelectedLocalItem = fi;
				await vm.UploadSelectedCommand.ExecuteAsync(null);
			}
		}
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		if (BindingContext is MainViewModel vm)
		{
			// Apply initial pane ratio
			ApplyPaneWidths(vm.PaneSplitRatio);
			// Apply terminal height
			TerminalContainer.HeightRequest = vm.TerminalHeight;
		}

	// Keyboard shortcut skipped due to cross-platform limitations; consider platform effect later.
	}

	// Splitter drag between panes
	private void OnPaneSplitterPanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.StatusType == GestureStatus.Running && ExplorerGrid.Width > 0)
		{
			var ratio = vm.PaneSplitRatio + (e.TotalX / ExplorerGrid.Width);
			ratio = Math.Max(0.1, Math.Min(0.9, ratio));
			vm.PaneSplitRatio = ratio;
			ApplyPaneWidths(ratio);
		}
	}

	// Terminal resize by dragging the handle
	private void OnTerminalHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.StatusType == GestureStatus.Running)
		{
			var newHeight = Math.Max(120, Math.Min(600, vm.TerminalHeight + (-e.TotalY)));
			vm.TerminalHeight = newHeight;
			TerminalContainer.HeightRequest = newHeight;
		}
	}

	private void ApplyPaneWidths(double ratio)
	{
		ExplorerGrid.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
		ExplorerGrid.ColumnDefinitions[2].Width = new GridLength(1 - ratio, GridUnitType.Star);
	}

	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
#if WINDOWS
		try
		{
			var page = this.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.Page;
			if (page is null) return;
			// Add Ctrl+` accelerator if not already present
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
					(BindingContext as MainViewModel)?.ToggleTerminalCommand.Execute(null);
					e.Handled = true;
				};
				page.KeyboardAccelerators.Add(accel);
			}
		}
		catch { }
#endif
	}
}
