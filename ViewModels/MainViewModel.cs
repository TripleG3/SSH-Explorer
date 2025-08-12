using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Renci.SshNet.Sftp;
using SSHExplorer.Models;
using SSHExplorer.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using SSHExplorer.Utilities;

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
    [ObservableProperty] private double terminalHeight = 220; // pixels
    [ObservableProperty] private double paneSplitRatio = 0.5; // 0..1 (left/remote width ratio)

    [ObservableProperty] private bool isConnected;

    [ObservableProperty]
    private SftpFile? selectedRemoteItem;

    partial void OnSelectedRemoteItemChanged(SftpFile? value)
    {
        // No-op: navigation now handled by double-click gesture
    }

    [ObservableProperty]
    private FileSystemInfo? selectedLocalItem;

    partial void OnSelectedLocalItemChanged(FileSystemInfo? value)
    {
        // No-op: navigation now handled by double-click gesture
    }

    [RelayCommand]
    private async Task OpenRemoteFolderAsync(SftpFile item)
    {
        if (item is null || !item.IsDirectory) return;
        try
        {
            var next = PathHelpers.CombineUnix(RemotePath, item.Name);
            await _ssh.ChangeDirectoryAsync(next);
            RemotePath = next;
            await RefreshRemoteAsync();
        }
        catch (Exception ex)
        {
            AppendOutput($"Open folder failed: {ex.Message}\n");
            await _dialogs.DisplayMessageAsync("Open Folder", $"Could not open the folder: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenLocalFolder(FileSystemInfo fsi)
    {
        try
        {
            if (fsi is DirectoryInfo d)
            {
                LocalPath = d.FullName;
                RefreshLocal();
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"Open local folder failed: {ex.Message}\n");
            _ = _dialogs.DisplayMessageAsync("Open Folder", "Could not open the folder on this device.");
        }
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
            // Try restore last used profile
            var lastProfileName = Preferences.Get(PrefKeys.LastProfileName, string.Empty);
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
            else if (SelectedProfile is null)
            {
                // Restore last used profile if available
                SelectedProfile = Profiles.FirstOrDefault(p => string.Equals(p.Name, lastProfileName, StringComparison.OrdinalIgnoreCase))
                                   ?? Profiles[0];
            }

            // Restore UI state
            IsTerminalVisible = Preferences.Get(PrefKeys.IsTerminalVisible, IsTerminalVisible);
            IsTerminalPinned = Preferences.Get(PrefKeys.IsTerminalPinned, IsTerminalPinned);
            TerminalHeight = Preferences.Get(PrefKeys.TerminalHeight, TerminalHeight);
            PaneSplitRatio = Math.Clamp(Preferences.Get(PrefKeys.PaneSplitRatio, PaneSplitRatio), 0.1, 0.9);
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
            Preferences.Set(PrefKeys.LastProfileName, SelectedProfile.Name);
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
        try
        {
            RemoteItems.Clear();
            var items = await _ssh.ListDirectoryAsync(RemotePath);
            foreach (var item in items)
            {
                if (item.Name is "." or "..") continue;
                RemoteItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            AppendOutput($"List directory failed: {ex.Message}\n");
            await _dialogs.DisplayMessageAsync("Browse", $"Could not load folder contents: {ex.Message}");
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
    private void TogglePin() => IsTerminalPinned = !IsTerminalPinned;

    [RelayCommand]
    private async Task NavigateRemoteAsync(SftpFile item)
    {
        if (item.IsDirectory)
        {
            try
            {
                var next = PathHelpers.CombineUnix(RemotePath, item.Name);
                await _ssh.ChangeDirectoryAsync(next);
                RemotePath = next;
                await RefreshRemoteAsync();
            }
            catch (Exception ex)
            {
                AppendOutput($"Open folder failed: {ex.Message}\n");
                await _dialogs.DisplayMessageAsync("Open Folder", $"Could not open the folder: {ex.Message}");
            }
        }
        else
        {
            // file actions
            var choice = await _dialogs.DisplayActionSheetAsync(item.Name, "Cancel", null, "Execute", "Download", "Delete", "Rename");
            switch (choice)
            {
                case "Execute":
            try { await ExecuteRemoteFile(item); }
            catch (Exception ex) { AppendOutput($"Execute failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Execute", "Could not execute the file."); }
                    break;
                case "Download":
            try { await DownloadRemoteFile(item); }
            catch (Exception ex) { AppendOutput($"Download failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Download", "Could not download the file."); }
                    break;
                case "Delete":
            try { await DeleteRemoteFile(item); }
            catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Delete", "Could not delete the file."); }
                    break;
                case "Rename":
            try { await RenameRemoteFile(item); }
            catch (Exception ex) { AppendOutput($"Rename failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Rename", "Could not rename the file."); }
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task NavigateLocal(FileSystemInfo fsi)
    {
        if (fsi is DirectoryInfo d)
        {
            try { LocalPath = d.FullName; RefreshLocal(); }
            catch (Exception ex) { AppendOutput($"Open failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Open", "Could not open the item."); }
            return;
        }

        // local file actions
        var choice = await _dialogs.DisplayActionSheetAsync(fsi.Name, "Cancel", null, "Open", "Upload", "Delete", "Rename");
        switch (choice)
        {
            case "Open":
                // For files, Open could be a no-op or platform open; here we do nothing
                break;
            case "Upload":
                if (fsi is FileInfo fi)
                {
                    try { await UploadLocalFile(fi); }
                    catch (Exception ex) { AppendOutput($"Upload failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Upload", "Could not upload the file."); }
                }
                break;
            case "Delete":
                try { await ConfirmAndDeleteLocalAsync(fsi); }
                catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Delete", "Could not delete the item."); }
                break;
            case "Rename":
                try { await RenameLocalAsync(fsi); }
                catch (Exception ex) { AppendOutput($"Rename failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Rename", "Could not rename the item."); }
                break;
        }
    }

    [RelayCommand]
    private async Task GoBackRemoteAsync()
    {
        var parent = PathHelpers.ParentUnix(RemotePath);
        if (parent != RemotePath)
        {
            try
            {
                await _ssh.ChangeDirectoryAsync(parent);
                RemotePath = parent;
                await RefreshRemoteAsync();
            }
            catch (Exception ex)
            {
                AppendOutput($"Open folder failed: {ex.Message}\n");
                await _dialogs.DisplayMessageAsync("Open Folder", $"Could not open the folder: {ex.Message}");
            }
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
    try { await _ssh.DownloadFileAsync(PathHelpers.CombineUnix(RemotePath, item.Name), dest); AppendOutput($"Downloaded {item.Name}\n"); RefreshLocal(); }
    catch (Exception ex) { AppendOutput($"Download failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Download", "Could not download the file."); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task UploadSelectedAsync()
    {
        var item = SelectedLocalItem as FileInfo; if (item is null) return;
    var dest = PathHelpers.CombineUnix(RemotePath, item.Name);
        IsBusy = true;
        try { await _ssh.UploadFileAsync(item.FullName, dest); AppendOutput($"Uploaded {item.Name}\n"); await RefreshRemoteAsync(); }
    catch (Exception ex) { AppendOutput($"Upload failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Upload", "Could not upload the file."); }
        finally { IsBusy = false; }
    }

    private async Task ExecuteRemoteFile(SftpFile file)
    {
    var ok = await _dialogs.DisplayAlertAsync("Execute", $"Execute {file.Name}?", "OK", "Cancel");
        if (!ok) return;
    await ExecuteCommandWithConfirm($"chmod +x '{PathHelpers.CombineUnix(RemotePath, file.Name)}' && '{PathHelpers.CombineUnix(RemotePath, file.Name)}'");
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
    try { await _ssh.ExecuteCommandAsync($"rm -f '{PathHelpers.CombineUnix(RemotePath, file.Name)}'"); await RefreshRemoteAsync(); }
        catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); }
    }

    private async Task RenameRemoteFile(SftpFile file)
    {
    var n = await _dialogs.DisplayPromptAsync("Rename", "New name:", initialValue: file.Name);
        if (string.IsNullOrWhiteSpace(n) || n == file.Name) return;
    try { await _ssh.ExecuteCommandAsync($"mv '{PathHelpers.CombineUnix(RemotePath, file.Name)}' '{PathHelpers.CombineUnix(RemotePath, n)}'"); await RefreshRemoteAsync(); }
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

    [RelayCommand]
    private async Task ShowRemoteActionsAsync(SftpFile item)
    {
        if (item is null) return;
        var isDir = item.IsDirectory;
        var list = isDir
            ? new[] { "Open", "Copy", "Move", "Delete", "Rename" }
            : new[] { "Execute", "Download", "Copy", "Move", "Delete", "Rename" };
        var choice = await _dialogs.DisplayActionSheetAsync(item.Name, "Close", null, list);
        switch (choice)
        {
            case "Open" when isDir:
                await OpenRemoteFolderAsync(item);
                break;
            case "Execute" when !isDir:
                await ExecuteRemoteFile(item);
                break;
            case "Download" when !isDir:
                await DownloadRemoteFile(item);
                break;
            case "Delete":
                await ConfirmAndDeleteRemoteAsync(item);
                break;
            case "Rename":
                await RenameRemoteFile(item);
                break;
            case "Copy":
                await CopyRemoteAsync(item);
                break;
            case "Move":
                await MoveRemoteAsync(item);
                break;
        }
    }

    [RelayCommand]
    private async Task ShowLocalActionsAsync(FileSystemInfo fsi)
    {
        if (fsi is null) return;
        var list = new[] { "Open", "Copy", "Move", "Delete", "Rename", "Upload" };
        var choice = await _dialogs.DisplayActionSheetAsync(fsi.Name, "Close", null, list);
        switch (choice)
        {
            case "Open":
                if (fsi is DirectoryInfo d) { LocalPath = d.FullName; RefreshLocal(); }
                break;
            case "Upload":
                if (fsi is FileInfo fi) await UploadLocalFile(fi);
                break;
            case "Delete":
                await ConfirmAndDeleteLocalAsync(fsi);
                break;
            case "Rename":
                await RenameLocalAsync(fsi);
                break;
            case "Copy":
                await CopyLocalAsync(fsi);
                break;
            case "Move":
                await MoveLocalAsync(fsi);
                break;
        }
    }

    private async Task ConfirmAndDeleteRemoteAsync(SftpFile item)
    {
        var ok = await _dialogs.DisplayAlertAsync("Confirm", $"Delete {(item.IsDirectory ? "folder" : "file")} {item.Name}?", "OK", "Cancel");
        if (!ok) return;
    var path = PathHelpers.CombineUnix(RemotePath, item.Name);
        var cmd = item.IsDirectory ? $"rm -rf '{path}'" : $"rm -f '{path}'";
    try { await _ssh.ExecuteCommandAsync(cmd); await RefreshRemoteAsync(); }
    catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Delete", "Could not delete the item."); }
    }

    private async Task CopyRemoteAsync(SftpFile item)
    {
    var src = PathHelpers.CombineUnix(RemotePath, item.Name);
        var dest = await _dialogs.DisplayPromptAsync("Copy", "Destination path:", "OK", "Cancel", initialValue: src);
        if (string.IsNullOrWhiteSpace(dest)) return;
        var ok = await _dialogs.DisplayAlertAsync("Confirm", $"cp -r '{src}' '{dest}'?", "OK", "Cancel");
        if (!ok) return;
    try { await _ssh.ExecuteCommandAsync($"cp -r '{src}' '{dest}'"); await RefreshRemoteAsync(); }
    catch (Exception ex) { AppendOutput($"Copy failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Copy", "Could not copy the item."); }
    }

    private async Task MoveRemoteAsync(SftpFile item)
    {
    var src = PathHelpers.CombineUnix(RemotePath, item.Name);
        var dest = await _dialogs.DisplayPromptAsync("Move", "Destination path:", "OK", "Cancel", initialValue: src);
        if (string.IsNullOrWhiteSpace(dest) || dest == src) return;
        var ok = await _dialogs.DisplayAlertAsync("Confirm", $"mv '{src}' '{dest}'?", "OK", "Cancel");
        if (!ok) return;
    try { await _ssh.ExecuteCommandAsync($"mv '{src}' '{dest}'"); await RefreshRemoteAsync(); }
    catch (Exception ex) { AppendOutput($"Move failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Move", "Could not move the item."); }
    }

    private async Task ConfirmAndDeleteLocalAsync(FileSystemInfo fsi)
    {
        var ok = await _dialogs.DisplayAlertAsync("Confirm", $"Delete {(fsi is DirectoryInfo ? "folder" : "file")} {fsi.Name}?", "OK", "Cancel");
        if (!ok) return;
        try
        {
            if (fsi is DirectoryInfo dd) dd.Delete(true); else File.Delete(fsi.FullName);
            RefreshLocal();
        }
    catch (Exception ex) { AppendOutput($"Delete failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Delete", "Could not delete the item."); }
    }

    private async Task RenameLocalAsync(FileSystemInfo fsi)
    {
        var n = await _dialogs.DisplayPromptAsync("Rename", "New name:", initialValue: fsi.Name);
        if (string.IsNullOrWhiteSpace(n) || n == fsi.Name) return;
        try
        {
            var target = Path.Combine(Path.GetDirectoryName(fsi.FullName)!, n);
            if (fsi is DirectoryInfo)
                Directory.Move(fsi.FullName, target);
            else
                File.Move(fsi.FullName, target, true);
            RefreshLocal();
        }
    catch (Exception ex) { AppendOutput($"Rename failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Rename", "Could not rename the item."); }
    }

    private async Task CopyLocalAsync(FileSystemInfo fsi)
    {
        var dest = await _dialogs.DisplayPromptAsync("Copy", "Destination path:", "OK", "Cancel", initialValue: fsi.FullName);
        if (string.IsNullOrWhiteSpace(dest) || dest == fsi.FullName) return;
        try
        {
            if (fsi is DirectoryInfo dd) CopyDirectoryRecursive(dd.FullName, dest);
            else File.Copy(fsi.FullName, dest, true);
            RefreshLocal();
        }
    catch (Exception ex) { AppendOutput($"Copy failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Copy", "Could not copy the item."); }
    }

    private async Task MoveLocalAsync(FileSystemInfo fsi)
    {
        var dest = await _dialogs.DisplayPromptAsync("Move", "Destination path:", "OK", "Cancel", initialValue: fsi.FullName);
        if (string.IsNullOrWhiteSpace(dest) || dest == fsi.FullName) return;
        try
        {
            if (fsi is DirectoryInfo)
                Directory.Move(fsi.FullName, dest);
            else
                File.Move(fsi.FullName, dest, true);
            RefreshLocal();
        }
    catch (Exception ex) { AppendOutput($"Move failed: {ex.Message}\n"); await _dialogs.DisplayMessageAsync("Move", "Could not move the item."); }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        var src = new DirectoryInfo(sourceDir);
        if (!src.Exists) return;
        Directory.CreateDirectory(destDir);
        foreach (var file in src.GetFiles())
        {
            var target = Path.Combine(destDir, file.Name);
            file.CopyTo(target, true);
        }
        foreach (var dir in src.GetDirectories())
        {
            CopyDirectoryRecursive(dir.FullName, Path.Combine(destDir, dir.Name));
        }
    }

    private void AppendOutput(string text) => TerminalOutput += text;

    partial void OnSelectedProfileChanged(Profile? value)
    {
        if (value is not null)
            Preferences.Set(PrefKeys.LastProfileName, value.Name);
    }

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        Preferences.Set(PrefKeys.IsTerminalVisible, value);
    }

    partial void OnIsTerminalPinnedChanged(bool value)
    {
        Preferences.Set(PrefKeys.IsTerminalPinned, value);
    }

    partial void OnTerminalHeightChanged(double value)
    {
        Preferences.Set(PrefKeys.TerminalHeight, value);
    }

    partial void OnPaneSplitRatioChanged(double value)
    {
        Preferences.Set(PrefKeys.PaneSplitRatio, Math.Clamp(value, 0.1, 0.9));
    }

    private static class PrefKeys
    {
        public const string LastProfileName = "LastProfileName";
        public const string IsTerminalVisible = "IsTerminalVisible";
        public const string IsTerminalPinned = "IsTerminalPinned";
        public const string TerminalHeight = "TerminalHeight";
        public const string PaneSplitRatio = "PaneSplitRatio";
    }
}
