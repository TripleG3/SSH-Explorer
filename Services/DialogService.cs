using System.Threading.Tasks;

namespace SSHExplorer.Services;

public sealed class DialogService : IDialogService
{
    private static Page GetPage()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page ?? Shell.Current?.CurrentPage;
        if (page is null) throw new InvalidOperationException("No active page available for dialog display.");
        return page;
    }

    public Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        => GetPage().DisplayAlert(title, message, accept, cancel);

    public Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = "", int maxLength = -1, string keyboard = "default", string initialValue = "")
        => GetPage().DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, Keyboard.Default, initialValue);

    public Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        => GetPage().DisplayActionSheet(title, cancel, destruction, buttons);
}
