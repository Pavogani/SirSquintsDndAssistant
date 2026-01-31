namespace SirSquintsDndAssistant.Services.Utilities;

public interface IDialogService
{
    Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null, int maxLength = -1, Keyboard? keyboard = null, string initialValue = "");
    Task DisplayAlertAsync(string title, string message, string cancel = "OK");
    Task<bool> DisplayConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
    Task<string?> DisplayActionSheetAsync(string title, string? cancel, string? destruction, params string[] buttons);
}

public class DialogService : IDialogService
{
    private Page? GetCurrentPage()
    {
        try
        {
            if (Application.Current?.Windows is { Count: > 0 } windows)
            {
                var window = windows[0];
                if (window?.Page is Shell shell)
                {
                    return shell.CurrentPage ?? shell;
                }
                return window?.Page;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting current page: {ex.Message}");
        }
        return null;
    }

    public async Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null, int maxLength = -1, Keyboard? keyboard = null, string initialValue = "")
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            System.Diagnostics.Debug.WriteLine("Cannot display prompt: No current page available");
            return null;
        }

        try
        {
            return await page.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard, initialValue);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying prompt: {ex.Message}");
            return null;
        }
    }

    public async Task DisplayAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            System.Diagnostics.Debug.WriteLine($"Cannot display alert: No current page available. Title: {title}, Message: {message}");
            return;
        }

        try
        {
            await page.DisplayAlert(title, message, cancel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying alert: {ex.Message}");
        }
    }

    public async Task<bool> DisplayConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No")
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            System.Diagnostics.Debug.WriteLine($"Cannot display confirm: No current page available. Title: {title}");
            return false;
        }

        try
        {
            return await page.DisplayAlert(title, message, accept, cancel);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying confirm: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> DisplayActionSheetAsync(string title, string? cancel, string? destruction, params string[] buttons)
    {
        var page = GetCurrentPage();
        if (page == null)
        {
            System.Diagnostics.Debug.WriteLine($"Cannot display action sheet: No current page available. Title: {title}");
            return null;
        }

        try
        {
            return await page.DisplayActionSheet(title, cancel, destruction, buttons);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying action sheet: {ex.Message}");
            return null;
        }
    }
}
