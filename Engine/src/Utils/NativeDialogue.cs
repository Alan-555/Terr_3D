using TinyDialogsNet;

namespace Terr3D.Utils;

/// <summary>
/// Provides access to native system dialogue boxes.
/// </summary>
public static class NativeDialogue
{
    /// <summary>
    /// Shows a simple system message box with an OK button.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">The title of the message box.</param>
    public static void ShowMessage(string message, string title = "Terr3D")
    {
        TinyDialogs.MessageBox(title, message, MessageBoxDialogType.Ok, MessageBoxIconType.Information, MessageBoxButton.Ok);
    }

    /// <summary>
    /// Shows a system error box.
    /// </summary>
    public static void ShowError(string message, string title = "Error")
    {
        TinyDialogs.MessageBox(title, message, MessageBoxDialogType.Ok, MessageBoxIconType.Error, MessageBoxButton.Ok);
    }

    /// <summary>
    /// Shows a message box asynchronously.
    /// </summary>
    public static async Task ShowMessageAsync(string message, string title = "Terr3D")
    {
        await Task.Run(() => ShowMessage(message, title));
    }
}
