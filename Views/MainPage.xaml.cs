using SSHExplorer.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System.ComponentModel;
using System.Linq;
using System.IO;
using SkiaSharp;

namespace SSHExplorer.Views;

public partial class MainPage : ContentPage
{
	private string? _pinnedIconPath;
	private string? _unpinnedIconPath;

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
			// Apply terminal height
			TerminalContainer.HeightRequest = vm.TerminalHeight;

			// Load icons and background from external folder (choose best-fit assets)
			try
			{
				var baseDir = @"C:\\Users\\micha\\OneDrive\\Pictures\\From Cory\\Logo";
				if (Directory.Exists(baseDir))
				{
					// Pick the best large landscape wallpaper as background
					var allImages = Directory.EnumerateFiles(baseDir, "*.*", SearchOption.AllDirectories)
						.Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
						.ToList();

					string? bestBackground = null;
					double bestScore = double.MinValue;
					foreach (var path in allImages)
					{
						try
						{
							using var stream = File.OpenRead(path);
							using var codec = SKCodec.Create(stream);
							if (codec == null) continue;
							var w = codec.Info.Width;
							var h = codec.Info.Height;
							if (w <= 0 || h <= 0) continue;
							double ratio = (double)w / h;
							// Prefer wide wallpapers ~16:9 and 4K+, bonus if under a 'Wallpaper' directory
							bool largeEnough = w >= 3840 && h >= 2000; // 4K-ish
							bool landscape = ratio >= 1.6 && ratio <= 2.4;
							bool isWallpaper = path.Contains("Wallpaper", StringComparison.OrdinalIgnoreCase);
							if (largeEnough && landscape)
							{
								double score = (w * h) / 1_000_000.0; // base on megapixels
								if (isWallpaper) score += 5; // wallpaper bonus
								// slight preference for neutral-themed files
								if (path.Contains("Gray", StringComparison.OrdinalIgnoreCase) || path.Contains("Grey", StringComparison.OrdinalIgnoreCase)) score += 0.5;
								if (score > bestScore)
								{
									bestScore = score;
									bestBackground = path;
								}
							}
						}
						catch { }
					}

					if (bestBackground == null)
					{
						// Fallback: any reasonably large landscape image
						foreach (var path in allImages)
						{
							try
							{
								using var stream = File.OpenRead(path);
								using var codec = SKCodec.Create(stream);
								if (codec == null) continue;
								var w = codec.Info.Width;
								var h = codec.Info.Height;
								if (w <= 0 || h <= 0) continue;
								double ratio = (double)w / h;
								bool largeEnough = w >= 1600 && h >= 900;
								bool landscape = ratio >= 1.5;
								if (largeEnough && landscape)
								{
									bestBackground = path;
									break;
								}
							}
							catch { }
						}
					}

					if (!string.IsNullOrEmpty(bestBackground))
					{
						this.BackgroundImageSource = ImageSource.FromFile(bestBackground);
						Application.Current!.Resources["BrandBackgroundPath"] = bestBackground;
					}

					// Choose a small, square icon (<=512px), prefer PNG
					string? smallIcon = null;
					double bestIconScore = double.MinValue;
					foreach (var path in allImages)
					{
						try
						{
							using var stream = File.OpenRead(path);
							using var codec = SKCodec.Create(stream);
							if (codec == null) continue;
							var w = codec.Info.Width;
							var h = codec.Info.Height;
							if (w <= 0 || h <= 0) continue;
							int maxDim = Math.Max(w, h);
							int minDim = Math.Min(w, h);
							bool small = maxDim <= 512 && maxDim >= 96;
							bool squareish = Math.Abs(w - h) <= Math.Max(10, maxDim / 20); // within 5%
							if (small && squareish)
							{
								double score = -maxDim; // prefer smaller among acceptable
								if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) score += 1; // prefer PNG
								if (score > bestIconScore)
								{
									bestIconScore = score;
									smallIcon = path;
								}
							}
						}
						catch { }
					}

					if (!string.IsNullOrEmpty(smallIcon))
					{
						Application.Current!.Resources["BrandSmallIconPath"] = smallIcon;
					}

					// Pin icons (if any specifically named); otherwise leave as-existing behavior
					_pinnedIconPath = Directory.EnumerateFiles(baseDir, "*pin*.*", SearchOption.AllDirectories)
									   .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
					_unpinnedIconPath = Directory.EnumerateFiles(baseDir, "*unpin*.*", SearchOption.AllDirectories)
										 .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
								  ?? _pinnedIconPath;
					UpdatePinIcon(vm.IsTerminalPinned);

					// React to pin state changes
					vm.PropertyChanged += OnVmPropertyChanged;
				}
			}
			catch { }
		}

	// Keyboard shortcut skipped due to cross-platform limitations; consider platform effect later.
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
		if (e.PropertyName == nameof(vm.IsTerminalPinned))
		{
			UpdatePinIcon(vm.IsTerminalPinned);
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
}
