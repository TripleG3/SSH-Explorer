namespace SSHExplorer.Models.Services;

public interface ITextEditorService : IStatePublisher<TextEditorState>
{
    Task LoadFileAsync(string filePath, CancellationToken ct = default);
    Task SaveFileAsync(CancellationToken ct = default);
    Task SaveAsFileAsync(string filePath, CancellationToken ct = default);
    Task NewFileAsync(CancellationToken ct = default);
    Task CloseFileAsync(CancellationToken ct = default);
    void UpdateContent(string content);
}