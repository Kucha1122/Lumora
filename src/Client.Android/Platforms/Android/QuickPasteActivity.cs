using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Lumora.Client.Android.Clipboard;
using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Sync;
using Button = Android.Widget.Button;
using ListView = Android.Widget.ListView;
using TextView = Android.Widget.TextView;

namespace Lumora.Client.Android;

/// <summary>
/// Small dialog-themed window opened by QuickTileService. Built with plain Android views
/// (no MAUI page) so it stays lightweight and doesn't require standing up the full Shell.
/// </summary>
[Activity(Theme = "@style/Theme.AppCompat.Dialog", Exported = false)]
public sealed class QuickPasteActivity : Activity
{
    private ClipboardSyncEngine? syncEngine;
    private AndroidClipboardBridge? clipboardBridge;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Title = "Lumora — schowek";

        var services = IPlatformApplication.Current?.Services;
        syncEngine = services?.GetService(typeof(ClipboardSyncEngine)) as ClipboardSyncEngine;
        clipboardBridge = services?.GetService(typeof(AndroidClipboardBridge)) as AndroidClipboardBridge;

        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetPadding(24, 24, 24, 24);

        var sendButton = new Button(this) { Text = "Wyślij mój schowek" };
        sendButton.Click += async (_, _) => await SendCurrentClipboardAsync();
        root.AddView(sendButton);

        var items = syncEngine?.History.Items ?? [];
        if (items.Count == 0)
        {
            var empty = new TextView(this) { Text = "Brak wpisów w historii." };
            root.AddView(empty);
        }
        else
        {
            var listView = new ListView(this);
            var labels = items.Select(Describe).ToArray();
            listView.Adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, labels);
            listView.ItemClick += async (_, e) => await CopyItemAsync(items[e.Position]);
            root.AddView(listView, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f));
        }

        SetContentView(root);
    }

    private static string Describe(ClipboardHistoryItem item)
    {
        if (item.Kind != LocalClipboardContentKind.Text)
        {
            return $"[obraz, {item.Plaintext.Length} B]";
        }

        var text = System.Text.Encoding.UTF8.GetString(item.Plaintext);
        return text.Length > 60 ? text[..60] + "…" : text;
    }

    private async Task CopyItemAsync(ClipboardHistoryItem item)
    {
        if (syncEngine is null)
        {
            return;
        }

        await syncEngine.PasteFromHistoryAsync(item, CancellationToken.None);
        Toast.MakeText(this, "Skopiowano do schowka.", ToastLength.Short)?.Show();
        Finish();
    }

    private async Task SendCurrentClipboardAsync()
    {
        var content = clipboardBridge?.TryCaptureCurrentClipboard();
        if (content is null)
        {
            Toast.MakeText(this, "Schowek telefonu jest pusty.", ToastLength.Short)?.Show();
            return;
        }

        if (clipboardBridge is not null)
        {
            await clipboardBridge.RaiseContentChangedAsync(content);
        }

        Toast.MakeText(this, "Wysłano.", ToastLength.Short)?.Show();
        Finish();
    }
}
