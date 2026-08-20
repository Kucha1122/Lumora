using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Sync;

namespace Lumora.Client.Desktop.Windows;

public sealed record QuickPasteRow(string Timestamp, string Preview, Bitmap? Thumbnail);

/// <summary>
/// Ctrl+\ quick-paste picker — shows the last 10 clipboard entries, Up/Down to move,
/// Enter to paste the selected one back into whatever window had focus, Escape to cancel.
/// </summary>
public partial class QuickPastePopup : Window
{
    private readonly List<ClipboardHistoryItem> items;
    private readonly Func<ClipboardHistoryItem, Task> onSelected;

    public QuickPastePopup(IReadOnlyList<ClipboardHistoryItem> items, Func<ClipboardHistoryItem, Task> onSelected)
    {
        InitializeComponent();
        this.items = items.ToList();
        this.onSelected = onSelected;

        EntriesList.ItemsSource = this.items
            .Select(i => new QuickPasteRow(i.CreatedAt.LocalDateTime.ToString("HH:mm:ss"), Describe(i), TryDecodeThumbnail(i)))
            .ToList();
        if (this.items.Count > 0)
        {
            EntriesList.SelectedIndex = 0;
        }

        // Tunnel + handledEventsToo: the ListBox has its own built-in arrow/Enter handling
        // that would otherwise swallow these keys (bubble routing stops once something
        // marks the event handled) before this window-level handler ever saw them —
        // that's exactly why Enter previously didn't reliably confirm the paste.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Deactivated += (_, _) => Close();
        Opened += (_, _) => EntriesList.Focus();
    }

    public void ShowAt(int screenX, int screenY)
    {
        Position = new PixelPoint(screenX, screenY);
        Show();
        Activate();
    }

    private static string Describe(ClipboardHistoryItem item)
    {
        if (item.Kind == LocalClipboardContentKind.Image)
        {
            return $"obraz ({item.Plaintext.Length} B)";
        }

        var text = System.Text.Encoding.UTF8.GetString(item.Plaintext);
        var preview = text.Length > 60 ? text[..60] + "…" : text;
        return preview.ReplaceLineEndings(" ");
    }

    private static Bitmap? TryDecodeThumbnail(ClipboardHistoryItem item)
    {
        if (item.Kind != LocalClipboardContentKind.Image)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(item.Plaintext);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                EntriesList.SelectedIndex = Math.Min(EntriesList.SelectedIndex + 1, items.Count - 1);
                e.Handled = true;
                break;
            case Key.Up:
                EntriesList.SelectedIndex = Math.Max(EntriesList.SelectedIndex - 1, 0);
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Enter:
                e.Handled = true;
                var index = EntriesList.SelectedIndex;
                Close();
                if (index >= 0 && index < items.Count)
                {
                    await onSelected(items[index]);
                }

                break;
        }
    }
}
