namespace SSHExplorer.Models.Services;

public sealed class TextEditorService : StatePublisher<TextEditorState>, ITextEditorService
{
    public TextEditorService() : base(TextEditorState.Empty)
    {
    }

    public async Task LoadFileAsync(string filePath, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });
        
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                SetState(State with { IsBusy = false, ErrorMessage = "File path cannot be empty" });
                return;
            }

            if (!File.Exists(filePath))
            {
                SetState(State with { IsBusy = false, ErrorMessage = "File not found" });
                return;
            }

            var content = await File.ReadAllTextAsync(filePath, ct);
            var isReadOnly = (File.GetAttributes(filePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            
            SetState(State with 
            { 
                IsBusy = false,
                FilePath = filePath,
                Content = content,
                HasChanges = false,
                IsReadOnly = isReadOnly,
                ErrorMessage = string.Empty
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task SaveFileAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(State.FilePath))
        {
            SetState(State with { ErrorMessage = "No file path specified. Use Save As instead." });
            return;
        }

        await SaveAsFileAsync(State.FilePath, ct);
    }

    public async Task SaveAsFileAsync(string filePath, CancellationToken ct = default)
    {
        SetState(State with { IsBusy = true, ErrorMessage = string.Empty });
        
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                SetState(State with { IsBusy = false, ErrorMessage = "File path cannot be empty" });
                return;
            }

            await File.WriteAllTextAsync(filePath, State.Content, ct);
            
            SetState(State with 
            { 
                IsBusy = false,
                FilePath = filePath,
                HasChanges = false,
                ErrorMessage = string.Empty
            });
        }
        catch (Exception ex)
        {
            SetState(State with { IsBusy = false, ErrorMessage = ex.Message });
        }
    }

    public async Task NewFileAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            SetState(TextEditorState.Empty);
        }, ct);
    }

    public async Task CloseFileAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            SetState(TextEditorState.Empty);
        }, ct);
    }

    public void UpdateContent(string content)
    {
        var hasChanges = !string.Equals(State.Content, content, StringComparison.Ordinal);
        SetState(State with { Content = content, HasChanges = hasChanges });
    }
}