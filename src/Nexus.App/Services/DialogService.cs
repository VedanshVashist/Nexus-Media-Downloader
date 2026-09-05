using System.IO;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace Nexus.App.Services;

/// <summary>File/folder pickers and modal confirmations, abstracted for the view-models.</summary>
public interface IDialogService
{
    /// <summary>Shows a folder picker; returns the chosen path or null if cancelled.</summary>
    string? PickFolder(string title, string? initialDirectory = null);

    /// <summary>Shows a file picker; returns the chosen path or null if cancelled.</summary>
    string? PickFile(string title, string filter, string? initialDirectory = null);

    /// <summary>Shows a yes/no confirmation. Returns true when the user confirms.</summary>
    bool Confirm(string message, string title = "Nexus");

    /// <summary>Shows a blocking error dialog (used sparingly; toasts are preferred).</summary>
    void ShowError(string message, string title = "Nexus");
}

/// <summary>WPF implementation using <see cref="Microsoft.Win32"/> common dialogs.</summary>
public sealed class DialogService : IDialogService
{
    public string? PickFolder(string title, string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? PickFile(string title, string filter, string? initialDirectory = null)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool Confirm(string message, string title = "Nexus")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowError(string message, string title = "Nexus")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
}
