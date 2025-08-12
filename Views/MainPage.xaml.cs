using SSHExplorer.ViewModels;

namespace SSHExplorer.Views;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
		Appearing += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
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
}
