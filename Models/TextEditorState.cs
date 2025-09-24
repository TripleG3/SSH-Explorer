namespace SSHExplorer.Models;

public readonly record struct TextEditorState(
    bool IsBusy,
    string FilePath,
    string Content,
    bool HasChanges,
    bool IsReadOnly,
    string ErrorMessage)
{
    public static readonly TextEditorState Empty = new(
        false,
        string.Empty,
        string.Empty,
        false,
        false,
        string.Empty);
}