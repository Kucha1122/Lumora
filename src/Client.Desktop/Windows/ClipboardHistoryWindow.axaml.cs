using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Sync;

namespace Lumora.Client.Desktop.Windows;

public sealed record ClipboardHistoryRow(Guid EntryId, string Timestamp, string Preview, Bitmap? Thumbnail);

public partial class ClipboardHistoryWindow : Window
{
    private readonly ClipboardHistoryStore history;
    private readonly ClipboardSyncEngine syncEngine;
    private readonly ActiveRoomStore activeRoom;
    private List<ClipboardHistoryItem> shown = [];

    // Decoding a PNG into a Bitmap on every Render() (which fires on every history change,
    // including ones unrelated to this entry) would re-decode the same image repeatedly for
    // no reason — cache by EntryId and only decode once per entry.
    private readonly Dictionary<Guid, Bitmap> thumbnailCache = [];

    public ClipboardHistoryWindow(ClipboardHistoryStore history, ClipboardSyncEngine syncEngine, ActiveRoomStore activeRoom)
    {
        InitializeComponent();
        this.history = history;
        this.syncEngine = syncEngine;
        this.activeRoom = activeRoom;

        history.Changed += OnHistoryChanged;
        Closed += (_, _) => history.Changed -= OnHistoryChanged;

        Render();
    }

    private void OnHistoryChanged() => Dispatcher.UIThread.Post(Render);

    private void Render()
    {
        var room = activeRoom.ActiveRoom;
        RoomLabel.Text = room is null
            ? "Historia schowka"
            : $"{(room.IsPrivate ? "🔒" : "🌐")} {room.DisplayName}";

        shown = history.Items.ToList();

        var liveIds = shown.Select(i => i.EntryId).ToHashSet();
        foreach (var staleId in thumbnailCache.Keys.Where(id => !liveIds.Contains(id)).ToList())
        {
            thumbnailCache.Remove(staleId);
        }

        EntriesList.ItemsSource = shown
            .Select(i => new ClipboardHistoryRow(i.EntryId, i.CreatedAt.LocalDateTime.ToString("HH:mm:ss"), Describe(i), GetThumbnail(i)))
            .ToList();
    }

    private Bitmap? GetThumbnail(ClipboardHistoryItem item)
    {
        if (item.Kind != LocalClipboardContentKind.Image)
        {
            return null;
        }

        if (thumbnailCache.TryGetValue(item.EntryId, out var cached))
        {
            return cached;
        }

        try
        {
            using var stream = new MemoryStream(item.Plaintext);
            var bitmap = new Bitmap(stream);
            thumbnailCache[item.EntryId] = bitmap;
            return bitmap;
        }
        catch
        {
            // Corrupted/unsupported image bytes shouldn't take down the whole history list —
            // just fall back to the generic text preview for this one entry.
            return null;
        }
    }

    private static string Describe(ClipboardHistoryItem item)
    {
        // No emoji/generic-icon prefix for images: the thumbnail itself already shows what
        // it is, so a leftover "🖼" would just be redundant clutter next to the real picture.
        if (item.Kind == LocalClipboardContentKind.Image)
        {
            return $"obraz ({item.Plaintext.Length} B)";
        }

        var text = System.Text.Encoding.UTF8.GetString(item.Plaintext);
        var preview = text.Length > 80 ? text[..80] + "…" : text;
        return preview.ReplaceLineEndings(" ");
    }

    private async void OnRowCopyClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid entryId })
        {
            return;
        }

        var item = shown.FirstOrDefault(i => i.EntryId == entryId);
        if (item is null)
        {
            return;
        }

        try
        {
            await syncEngine.PasteFromHistoryAsync(item, CancellationToken.None);
        }
        catch (Exception ex)
        {
            ShowStatus($"Nie udało się skopiować: {ex.Message}");
        }
    }

    private async void OnDeleteEntryClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid entryId })
        {
            return;
        }

        try
        {
            await syncEngine.DeleteEntryAsync(entryId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            ShowStatus($"Nie udało się usunąć wpisu: {ex.Message}");
        }
    }

    private async void OnClearAllClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            ClearAllButton.IsEnabled = false;
            await syncEngine.ClearHistoryAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            ShowStatus($"Nie udało się wyczyścić historii: {ex.Message}");
        }
        finally
        {
            ClearAllButton.IsEnabled = true;
        }
    }

    private void ShowStatus(string message) => RoomLabel.Text = message;
}
