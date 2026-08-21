using Android.Content;
using Android.Widget;
using AndroidUri = Android.Net.Uri;
using Lumora.Client.Android.Clipboard;
using Lumora.Client.Core.Clipboard;

namespace Lumora.Client.Android.Sharing;

/// <summary>
/// Reached from MainActivity when the system "Udostępnij → Lumora" share sheet delivers text
/// or an image (see plan §Share target). Pushes the content through the same
/// AndroidClipboardBridge.ContentChanged path as the in-app "Wyślij schowek" button — the app
/// doesn't need to be brought to the foreground for this to work, just alive enough for its DI
/// container (IPlatformApplication.Current) to be resolvable, which MAUI guarantees once
/// MainActivity.OnCreate has run.
/// </summary>
internal static class ShareTargetHandler
{
    public static async void HandleSharedTextAsync(string text)
    {
        var bridge = ResolveBridge();
        if (bridge is null)
        {
            return;
        }

        var content = new LocalClipboardContent(LocalClipboardContentKind.Text, System.Text.Encoding.UTF8.GetBytes(text));
        await bridge.RaiseContentChangedAsync(content);
        Toast.MakeText(global::Android.App.Application.Context, "Wysłano do Lumory", ToastLength.Short)?.Show();
    }

    public static async void HandleSharedImageAsync(Context context, AndroidUri uri)
    {
        var bridge = ResolveBridge();
        if (bridge is null)
        {
            return;
        }

        var bytes = ClipboardImageFile.ReadFromUri(context, uri);
        if (bytes is null)
        {
            return;
        }

        var content = new LocalClipboardContent(LocalClipboardContentKind.Image, bytes);
        await bridge.RaiseContentChangedAsync(content);
        Toast.MakeText(global::Android.App.Application.Context, "Wysłano do Lumory", ToastLength.Short)?.Show();
    }

    private static AndroidClipboardBridge? ResolveBridge() =>
        IPlatformApplication.Current?.Services.GetService(typeof(AndroidClipboardBridge)) as AndroidClipboardBridge;
}
