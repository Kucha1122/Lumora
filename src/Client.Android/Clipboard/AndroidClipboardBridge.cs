using Android.Content;
using AndroidClipData = Android.Content.ClipData;
using AndroidUri = Android.Net.Uri;
using Lumora.Client.Core.Clipboard;

namespace Lumora.Client.Android.Clipboard;

/// <summary>
/// Android implementation of <see cref="IClipboardBridge"/>. Writing to the clipboard is
/// unrestricted on Android, so <see cref="SetContentAsync"/> always works. Reading is not:
/// since Android 10, ClipboardManager.PrimaryClip returns null unless the caller has input
/// focus — see plan's "Ograniczenie platformy". ContentChanged is therefore never raised by
/// a background listener; callers (Schowek page, share target, quick tile) instead call
/// <see cref="TryCaptureCurrentClipboard"/> explicitly at moments they know the app has focus.
/// </summary>
public sealed class AndroidClipboardBridge : IClipboardBridge
{
    private readonly Context context;
    private ClipboardManager? manager;

    public AndroidClipboardBridge(Context context) => this.context = context;

    public event Func<LocalClipboardContent, Task>? ContentChanged;

    public void Start() => manager = context.GetSystemService(Context.ClipboardService) as ClipboardManager;

    public void Stop() => manager = null;

    public Task SetContentAsync(LocalClipboardContent content, CancellationToken ct)
    {
        if (manager is null)
        {
            return Task.CompletedTask;
        }

        AndroidClipData clip = content.Kind switch
        {
            LocalClipboardContentKind.Text => AndroidClipData.NewPlainText(
                "Lumora", System.Text.Encoding.UTF8.GetString(content.Data))!,
            LocalClipboardContentKind.Image => AndroidClipData.NewUri(
                context.ContentResolver, "Lumora", ClipboardImageFile.WriteToCache(context, content.Data))!,
            _ => throw new ArgumentOutOfRangeException(nameof(content))
        };

        manager.PrimaryClip = clip;
        return Task.CompletedTask;
    }

    /// <summary>Reads the current clipboard content, or null if empty/unreadable (no focus,
    /// unsupported mime type). Call only when the app is known to have input focus.</summary>
    public LocalClipboardContent? TryCaptureCurrentClipboard()
    {
        var clip = manager?.PrimaryClip;
        if (clip is null || clip.ItemCount == 0)
        {
            return null;
        }

        var item = clip.GetItemAt(0);
        if (item is null)
        {
            return null;
        }

        var uri = item.Uri;
        if (uri is not null)
        {
            var bytes = ClipboardImageFile.ReadFromUri(context, uri);
            return bytes is null ? null : new LocalClipboardContent(LocalClipboardContentKind.Image, bytes);
        }

        var text = item.CoerceToText(context)?.ToString();
        return string.IsNullOrEmpty(text)
            ? null
            : new LocalClipboardContent(LocalClipboardContentKind.Text, System.Text.Encoding.UTF8.GetBytes(text));
    }

    /// <summary>Raised only by explicit capture points, never by a background OS listener —
    /// see the class remarks. Exposed so pages can push what TryCaptureCurrentClipboard finds
    /// through the same ClipboardSyncEngine path as an OS-detected change would.</summary>
    internal Task RaiseContentChangedAsync(LocalClipboardContent content) =>
        ContentChanged?.Invoke(content) ?? Task.CompletedTask;
}
