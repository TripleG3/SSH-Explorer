using SSHExplorer.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;
using System.Linq;
using System.IO;

namespace SSHExplorer.Views;

public partial class MainPage : ContentPage
{
	private string? _pinnedIconPath;
	private string? _unpinnedIconPath;

	public MainPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Appearing += (_, _) => vm.LoadCommand.Execute(null);
		this.Loaded += OnLoaded;
	}

	private async void OnCommandsClicked(object? sender, EventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		var choice = await DisplayActionSheet("Popular Commands", "Cancel", null, "ls -la", "df -h", "top -b -n 1");
		if (string.IsNullOrEmpty(choice) || choice == "Cancel") return;
		var ok = await DisplayAlert("Confirm", choice, "OK", "Cancel");
		if (!ok) return;
		// Execute command through the terminal service
		await vm.ExecuteCommandAsync(choice);
	}

	// Drag from remote item -> drop on local pane = download
	private void OnRemoteDragStarting(object sender, DragStartingEventArgs e)
	{
		if (sender is not BindableObject bo || bo.BindingContext is not Renci.SshNet.Sftp.SftpFile sftp) return;
		e.Data.Properties["remoteName"] = sftp.Name;
	}

	private void OnLocalDrop(object sender, DropEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.Data.Properties.TryGetValue("remoteName", out var val) && val is string remoteName)
		{
			// trigger download of selected remote item
			var match = vm.FileExplorerState.RemoteItems.FirstOrDefault(r => r.Name == remoteName);
			if (match != null)
			{
				// Note: This needs to be implemented in the service or ViewModel
				// For now, we'll skip this functionality until proper commands are created
			}
		}
	}

	// Drag from local item -> drop on remote pane = upload
	private void OnLocalDragStarting(object sender, DragStartingEventArgs e)
	{
		if (sender is not BindableObject bo || bo.BindingContext is not FileSystemInfo fsi) return;
		e.Data.Properties["localPath"] = fsi.FullName;
	}

	private void OnRemoteDrop(object sender, DropEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.Data.Properties.TryGetValue("localPath", out var val) && val is string localPath)
		{
			var fi = new FileInfo(localPath);
			if (fi.Exists)
			{
				// Note: This needs to be implemented in the service or ViewModel
				// For now, we'll skip this functionality until proper commands are created
			}
		}
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		if (BindingContext is MainViewModel vm)
		{
			// Apply terminal height
			TerminalContainer.HeightRequest = vm.TerminalState.Height;

			// Update pin icon
			UpdatePinIcon(vm.TerminalState.IsPinned);

			// React to pin state changes
			vm.PropertyChanged += OnVmPropertyChanged;
		}

	// Keyboard shortcut skipped due to cross-platform limitations; consider platform effect later.
	}

	// Terminal resize by dragging the handle
	private void OnTerminalHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
	{
		if (BindingContext is not MainViewModel vm) return;
		if (e.StatusType == GestureStatus.Running)
		{
			var newHeight = Math.Max(120, Math.Min(600, vm.TerminalState.Height + (-e.TotalY)));
			// Note: This needs to be implemented via a service method
			// For now, we'll just update the UI directly
			TerminalContainer.HeightRequest = newHeight;
		}
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

	private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (sender is not MainViewModel vm) return;
		if (e.PropertyName == nameof(vm.TerminalState))
		{
			UpdatePinIcon(vm.TerminalState.IsPinned);
		}
	}

	private void UpdatePinIcon(bool pinned)
	{
		try
		{
			var path = pinned ? _pinnedIconPath : _unpinnedIconPath;
			if (!string.IsNullOrEmpty(path))
				PinButton.Source = ImageSource.FromFile(path);
		}
		catch { }
	}

	private async void OnAboutClicked(object? sender, EventArgs e)
	{
		var company = "Triple G3";
		var version = AppInfo.Current?.VersionString ?? "Unknown";
		await DisplayAlert("About", $"Company: {company}\nVersion: {version}", "OK");
	}

	private async void OnOptionsClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("OptionsPage");
	}
}
