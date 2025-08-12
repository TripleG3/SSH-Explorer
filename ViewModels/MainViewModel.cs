using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Renci.SshNet.Sftp;
using SSHExplorer.Models;
using SSHExplorer.Services;
using System.Collections.ObjectModel;

namespace SSHExplorer.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ISshService _ssh;
    private readonly IProfileService _profiles;
    private readonly IDialogService _dialogs;

    public ObservableCollection<Profile> Profiles { get; } = new();

    [ObservableProperty] private Profile? selectedProfile;

    [ObservableProperty] private string localPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    [ObservableProperty] private string remotePath = "/";

    public ObservableCollection<SftpFile> RemoteItems { get; } = new();
    public ObservableCollection<FileSystemInfo> LocalItems { get; } = new();

    [ObservableProperty] private string terminalOutput = string.Empty;
    [ObservableProperty] private string terminalInput = string.Empty;
    [ObservableProperty] private bool isTerminalVisible = true;
    [ObservableProperty] private bool isTerminalPinned = false;

    [ObservableProperty] private bool isConnected;

    [ObservableProperty]
    private SftpFile? selectedRemoteItem;

    partial void OnSelectedRemoteItemChanged(SftpFile? value)
    {
        // Treat selection as activation: open folders or show actions for files
        if (value is null) return;
        _ = NavigateRemoteAsync(value);
    }

    [ObservableProperty]
    private FileSystemInfo? selectedLocalItem;

    partial void OnSelectedLocalItemChanged(FileSystemInfo? value)
    {
        if (value is null) return;
        NavigateLocal(value);
    }

    private readonly IThemeService _theme;

    public MainViewModel(ISshService ssh, IProfileService profiles, IThemeService theme, IDialogService dialogs)
    {
        _ssh = ssh; _profiles = profiles; _theme = theme; _dialogs = dialogs;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return; IsBusy = true;
        try
        {
            // Apply TripleG3 theming from provided folder
            var themeFolder = @"C:\\Users\\micha\\OneDrive\\Pictures\\From Cory\\Logo";
            var primary = _theme is ThemeService ts ? ts.ComputePrimaryFromFolder(themeFolder) : Colors.CornflowerBlue;
            _theme.ApplyDarkTheme(primary);

            Profiles.Clear();
            foreach (var p in await _profiles.LoadAsync()) Profiles.Add(p);
            if (Profiles.Count == 0)
            {
                // Prompt for first profile
                var created = await CreateProfileInteractiveAsync();
                if (created is not null)
                {
                    Profiles.Add(created);
                    SelectedProfile = created;
                }
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        // If no profile selected, guide the user to create one
        if (SelectedProfile is null)
        {
            var created = await CreateProfileInteractiveAsync();
            if (created is null) return; // user cancelled
            Profiles.Add(created);
            SelectedProfile = created;
        }
        if (IsBusy) return; IsBusy = true;
        try
        {
            await _ssh.ConnectAsync(SelectedProfile);
            RemotePath = SelectedProfile.DefaultRemotePath;
            LocalPath = SelectedProfile.DefaultLocalPath;
            await RefreshRemoteAsync();
            RefreshLocal();
            AppendOutput($"Connected to {SelectedProfile.Host}\n");
            IsConnected = _ssh.IsConnected;
        }
        catch (Exception ex)
        {
            AppendOutput($"Error: {ex.Message}\n");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (!_ssh.IsConnected) return;
        var ok = await _dialogs.DisplayAlertAsync("Disconnect", "Disconnect from current SSH session?", "OK", "Cancel");
        if (!ok) return;
        try
        {
            await _ssh.DisconnectAsync();
            IsConnected = _ssh.IsConnected;
            AppendOutput("Disconnected\n");
        }
        catch (Exception ex)
        {
            AppendOutput($"Error: {ex.Message}\n");
        }
    }

    private async Task<Profile?> CreateProfileInteractiveAsync()
    {
        // Gather minimal profile info via dialogs; return null if cancelled at the host stage
        var name = await _dialogs.DisplayPromptAsync("Create Profile", "Enter a profile name:", "OK", "Cancel", "Default");
        if (string.IsNullOrWhiteSpace(name)) name = "Default";

        var host = await _dialogs.DisplayPromptAsync("Create Profile", "Host name or IP:", "OK", "Cancel");
        if (string.IsNullOrWhiteSpace(host)) return null;

        var user = await _dialogs.DisplayPromptAsync("Create Profile", "Username:", "OK", "Cancel");
        if (string.IsNullOrWhiteSpace(user)) user = Environment.UserName;

    var portStr = await _dialogs.DisplayPromptAsync("Create Profile", "Port:", "OK", "Cancel", initialValue: "22");
    var pass = await _dialogs.DisplayPromptAsync("Create Profile", "Password (leave blank if using key):", "OK", "Cancel");

        var profile = new Profile
        {
            Name = name!,
            Host = host!,
            Username = user!,
            Password = string.IsNullOrEmpty(pass) ? null : pass,
            Port = int.TryParse(portStr, out var portVal) ? portVal : 22
        };

        await _profiles.AddOrUpdateAsync(profile);
        return profile;
    }

    [RelayCommand]
    private async Task RefreshRemoteAsync()
    {
        if (!_ssh.IsConnected) return;
        RemoteItems.Clear();
        foreach (var item in await _ssh.ListDirectoryAsync(RemotePath))
        {
            if (item.Name is "." or "..") continue;
            RemoteItems.Add(item);
        }
    }

    [RelayCommand]
    private void RefreshLocal()
    {
        LocalItems.Clear();
        var di = new DirectoryInfo(LocalPath);
        if (!di.Exists) return;
        foreach (var d in di.EnumerateFileSystemInfos()) LocalItems.Add(d);
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(TerminalInput)) return;
        AppendOutput($"> {TerminalInput}\n");
        var result = await _ssh.ExecuteCommandAsync(TerminalInput);
        AppendOutput(result + "\n");
        TerminalInput = string.Empty;
    }

    public async Task<string> ExecuteCommandAsync(string cmd)
    {
        var result = await _ssh.ExecuteCommandAsync(cmd);
        AppendOutput($"> {cmd}\n{result}\n");
        return result;
    }

    [RelayCommand]
    private void ToggleTerminal() => IsTerminalVisible = !IsTerminalVisible;

    [RelayCommand]
    private void CollapseTerminalIfNotPinned()
    {
        if (!IsTerminalPinned)
            IsTerminalVisible = false;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
    var current = Application.Current!.RequestedTheme;
    var primary = (Color)Application.Current!.Resources["PrimaryColor"];
        if (current == AppTheme.Dark) _theme.ApplyLightTheme(primary); else _theme.ApplyDarkTheme(primary);
    }

    [RelayCommand]
    private async Task NavigateRemoteAsync(SftpFile item)
    {
        if (item.IsDirectory)
        {
            var next = CombineUnix(RemotePath, item.Name);
            await _ssh.ChangeDirectoryAsync(next);
            RemotePath = next;
            await RefreshRemoteAsync();
        }
        else
        {
            // file actions
            var choice = await _dialogs.DisplayActionSheetAsync(item.Name, "Cancel", null, "Execute", "Download", "Delete", "Rename");
            switch (choice)
            {
                case "Execute":
                    await ExecuteRemoteFile(item);
                    break;
                case "Download":
                    await DownloadRemoteFile(item);
                    break;
                case "Delete":
                    await DeleteRemoteFile(item);
                    break;
                case "Rename":
                    await RenameRemoteFile(item);
                    break;
            }
        }
    }

    [RelayCommand]
    private void NavigateLocal(FileSystemInfo fsi)
    {
        if (fsi is DirectoryInfo d)
        {
            LocalPath = d.FullName;
            RefreshLocal();
        }
        else
        {
            // local file actions
            var _ = _dialogs.DisplayActionSheetAsync(fsi.Name, "Cancel", null, "Open", "Upload", "Delete", "Rename").ContinueWith(async t =>
            {
                var choice = t.Result;
                switch (choice)
                {
                    case "Upload":
                        await UploadLocalFile((FileInfo)fsi);
                        break;
                    case "Delete":
                        try { File.Delete(fsi.FullName); RefreshLocal(); }
                        catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); }
                        break;
                    case "Rename":
                        var n = await _dialogs.DisplayPromptAsync("Rename", "New name:", initialValue: fsi.Name);
                        if (!string.IsNullOrWhiteSpace(n))
                        {
                            var target = Path.Combine(Path.GetDirectoryName(fsi.FullName)!, n);
                            File.Move(fsi.FullName, target, true);
                            RefreshLocal();
                        }
                        break;
                }
            });
        }
    }

    [RelayCommand]
    private async Task GoBackRemoteAsync()
    {
        var parent = ParentUnix(RemotePath);
        if (parent != RemotePath)
        {
            await _ssh.ChangeDirectoryAsync(parent);
            RemotePath = parent;
            await RefreshRemoteAsync();
        }
    }

    [RelayCommand]
    private void GoBackLocal()
    {
        var parent = Directory.GetParent(LocalPath)?.FullName;
        if (!string.IsNullOrEmpty(parent)) { LocalPath = parent; RefreshLocal(); }
    }

    [RelayCommand]
    private async Task DownloadSelectedAsync()
    {
        var item = SelectedRemoteItem; if (item is null || item.IsDirectory) return;
        var dest = Path.Combine(LocalPath, item.Name);
        IsBusy = true;
        try { await _ssh.DownloadFileAsync(CombineUnix(RemotePath, item.Name), dest); AppendOutput($"Downloaded {item.Name}\n"); RefreshLocal(); }
        catch (Exception ex) { AppendOutput($"Download failed: {ex.Message}\n"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task UploadSelectedAsync()
    {
        var item = SelectedLocalItem as FileInfo; if (item is null) return;
        var dest = CombineUnix(RemotePath, item.Name);
        IsBusy = true;
        try { await _ssh.UploadFileAsync(item.FullName, dest); AppendOutput($"Uploaded {item.Name}\n"); await RefreshRemoteAsync(); }
        catch (Exception ex) { AppendOutput($"Upload failed: {ex.Message}\n"); }
        finally { IsBusy = false; }
    }

    private async Task ExecuteRemoteFile(SftpFile file)
    {
    var ok = await _dialogs.DisplayAlertAsync("Execute", $"Execute {file.Name}?", "OK", "Cancel");
        if (!ok) return;
        await ExecuteCommandWithConfirm($"chmod +x '{CombineUnix(RemotePath, file.Name)}' && '{CombineUnix(RemotePath, file.Name)}'");
    }

    private async Task DownloadRemoteFile(SftpFile file)
    {
        SelectedRemoteItem = file; await DownloadSelectedAsync();
    }

    private async Task DeleteRemoteFile(SftpFile file)
    {
        if (!_ssh.IsConnected) return;
    var ok = await _dialogs.DisplayAlertAsync("Delete", $"Delete {file.Name}?", "OK", "Cancel");
        if (!ok) return;
        try { await _ssh.ExecuteCommandAsync($"rm -f '{CombineUnix(RemotePath, file.Name)}'"); await RefreshRemoteAsync(); }
        catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); }
    }

    private async Task RenameRemoteFile(SftpFile file)
    {
    var n = await _dialogs.DisplayPromptAsync("Rename", "New name:", initialValue: file.Name);
        if (string.IsNullOrWhiteSpace(n) || n == file.Name) return;
        try { await _ssh.ExecuteCommandAsync($"mv '{CombineUnix(RemotePath, file.Name)}' '{CombineUnix(RemotePath, n)}'"); await RefreshRemoteAsync(); }
        catch (Exception ex) { AppendOutput($"Rename failed: {ex.Message}\n"); }
    }

    private async Task UploadLocalFile(FileInfo file)
    {
        SelectedLocalItem = file; await UploadSelectedAsync();
    }

    private async Task ExecuteCommandWithConfirm(string cmd)
    {
    var ok = await _dialogs.DisplayAlertAsync("Confirm", cmd, "OK", "Cancel");
        if (!ok) return;
        var result = await _ssh.ExecuteCommandAsync(cmd);
        AppendOutput(result + "\n");
    }

    private void AppendOutput(string text) => TerminalOutput += text;

    private static string CombineUnix(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b;
        if (a.EndsWith('/')) return a + b;
        return a + "/" + b;
    }

    private static string ParentUnix(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return "/";
        var idx = path.TrimEnd('/').LastIndexOf('/');
        return idx <= 0 ? "/" : path[..idx];
    }
}
