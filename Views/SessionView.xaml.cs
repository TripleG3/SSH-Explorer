using SSHExplorer.ViewModels;
using Microsoft.Maui.Controls;

namespace SSHExplorer.Views;

public partial class SessionView : ContentView
{
    public SessionView()
    {
        InitializeComponent();
    }

    // Context menu handlers for files and directories
    private async void OnFileContextMenu(object? sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext == null)
            return;

        var viewModel = (SessionViewModel?)BindingContext;
        if (viewModel == null) return;

        var actions = new List<string> { "Open", "Edit", "Download", "Delete", "Rename", "Properties" };
        var result = await Shell.Current.DisplayActionSheet("File Actions", "Cancel", null, actions.ToArray());
        
        // Handle the selected action
        switch (result)
        {
            case "Open":
                // Handle open file
                break;
            case "Edit":
                // Handle edit file
                break;
            case "Download":
                // Handle download file
                break;
            case "Delete":
                // Handle delete file
                break;
            case "Rename":
                // Handle rename file
                break;
            case "Properties":
                // Handle show properties
                break;
        }
    }

    private async void OnDirectoryContextMenu(object? sender, EventArgs e)
    {
        if (sender is not Element element || element.BindingContext == null)
            return;

        var viewModel = (SessionViewModel?)BindingContext;
        if (viewModel == null) return;

        var actions = new List<string> { "Open", "Upload File", "Create Folder", "Delete", "Rename", "Properties" };
        var result = await Shell.Current.DisplayActionSheet("Directory Actions", "Cancel", null, actions.ToArray());
        
        // Handle the selected action
        switch (result)
        {
            case "Open":
                // Handle open directory
                break;
            case "Upload File":
                // Handle upload file
                break;
            case "Create Folder":
                // Handle create folder
                break;
            case "Delete":
                // Handle delete directory
                break;
            case "Rename":
                // Handle rename directory
                break;
            case "Properties":
                // Handle show properties
                break;
        }
    }

    // Drag and drop handlers
    private void OnDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is not Element element || element.BindingContext == null)
            return;

        e.Data.Text = element.BindingContext.ToString();
    }

    private void OnDrop(object? sender, DropEventArgs e)
    {
        // Handle file drop for uploads
    }

    // Terminal panning for resize
    private void OnTerminalHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (sender is not View handle) return;

        var terminalContainer = handle.Parent?.Parent as Grid;
        if (terminalContainer == null) return;

        var currentHeight = terminalContainer.HeightRequest;
        var newHeight = Math.Max(100, currentHeight - e.TotalY);
        terminalContainer.HeightRequest = Math.Min(400, newHeight);
    }
}