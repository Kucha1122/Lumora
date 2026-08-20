using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Lumora.Client.Desktop.Windows;

/// <summary>
/// Modal password prompt for joining a private room. <paramref name="onSubmit"/> does the
/// actual join and returns an error message on failure (window stays open, shows it inline)
/// or null on success (window closes).
/// </summary>
public partial class PasswordPromptWindow : Window
{
    private readonly Func<string, Task<string?>> onSubmit;

    /// <summary>True once <paramref name="onSubmit"/> has returned null (success) and this
    /// window has closed itself — check this after ShowDialog returns.</summary>
    public bool Succeeded { get; private set; }

    public PasswordPromptWindow(string roomDisplayName, Func<string, Task<string?>> onSubmit)
    {
        InitializeComponent();
        Icon = TrayIconFactory.BrandIcon;
        this.onSubmit = onSubmit;
        PromptText.Text = $"Hasło do „{roomDisplayName}”";
        Opened += (_, _) => PasswordBox.Focus();
    }

    private async void OnSubmitClicked(object? sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Text;
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Podaj hasło.");
            return;
        }

        SubmitButton.IsEnabled = false;
        var error = await onSubmit(password);
        SubmitButton.IsEnabled = true;

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        Succeeded = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close();
}
