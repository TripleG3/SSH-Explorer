using System.Threading.Tasks;

namespace SSHExplorer.Services;

public interface IDialogService
{
    Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel);
    Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = "", int maxLength = -1, string keyboard = "default", string initialValue = "");
    Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons);
}
