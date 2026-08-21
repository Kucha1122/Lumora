using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Lumora.Client.Android.Clipboard;
using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Sync;
using Button = Android.Widget.Button;
using ListView = Android.Widget.ListView;
using TextView = Android.Widget.TextView;
using Color = Android.Graphics.Color;
using View = Android.Views.View;
using Orientation = Android.Widget.Orientation;

namespace Lumora.Client.Android;

/// <summary>
/// Small floating window opened by QuickTileService. Built with plain Android views (no MAUI
/// page) so it stays lightweight and doesn't require standing up the full Shell.
///
/// A true "panel that expands out of the Quick Settings tile without navigating away" — like
/// Samsung's own Modes tile — isn't available to third-party apps: that's a One UI-private
/// extension, not part of the public TileService API. startActivityAndCollapse (an Activity)
/// is the only public mechanism. This styles that Activity to read as close to a native
/// floating panel as the platform allows: rounded dark card, dimmed backdrop, brand colors,
/// instead of the default plain white AppCompat dialog.
/// </summary>
[Activity(Theme = "@style/Theme.AppCompat.Dialog", Exported = false)]
public sealed class QuickPasteActivity : Activity
{
    private static readonly Color SurfaceColor = Color.ParseColor("#262B52");
    private static readonly Color BackgroundColor = Color.ParseColor("#1B1F3B");
    private static readonly Color AccentColor = Color.ParseColor("#8C7CF5");
    private static readonly Color MutedTextColor = Color.ParseColor("#9AA0C3");

    private ClipboardSyncEngine? syncEngine;
    private AndroidClipboardBridge? clipboardBridge;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Window?.SetDimAmount(0.6f);
        Window?.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        Window?.SetGravity(GravityFlags.Bottom);
        Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

        var services = IPlatformApplication.Current?.Services;
        syncEngine = services?.GetService(typeof(ClipboardSyncEngine)) as ClipboardSyncEngine;
        clipboardBridge = services?.GetService(typeof(AndroidClipboardBridge)) as AndroidClipboardBridge;

        var card = new LinearLayout(this) { Orientation = Orientation.Vertical };
        var cardMargin = (int)(16 * Resources!.DisplayMetrics!.Density);
        card.SetPadding(cardMargin, cardMargin, cardMargin, cardMargin);
        card.Background = RoundedCard(BackgroundColor, radiusDp: 24);

        var title = new TextView(this)
        {
            Text = "Lumora — schowek",
            TextSize = 16
        };
        title.SetTextColor(Color.White);
        title.SetTypeface(title.Typeface, TypefaceStyle.Bold);
        card.AddView(title, MarginParams(bottom: 12));

        var sendButton = new Button(this) { Text = "Wyślij mój schowek" };
        sendButton.SetTextColor(BackgroundColor);
        sendButton.Background = RoundedCard(AccentColor, radiusDp: 12);
        sendButton.Click += async (_, _) => await SendCurrentClipboardAsync();
        card.AddView(sendButton, MarginParams(bottom: 12));

        var items = syncEngine?.History.Items ?? [];
        if (items.Count == 0)
        {
            var empty = new TextView(this) { Text = "Brak wpisów w historii." };
            empty.SetTextColor(MutedTextColor);
            card.AddView(empty);
        }
        else
        {
            var listView = new ListView(this) { DividerHeight = (int)(8 * Resources.DisplayMetrics.Density) };
            listView.Divider = null;
            var adapter = new HistoryAdapter(this, items, SurfaceColor);
            listView.Adapter = adapter;
            listView.ItemClick += async (_, e) => await CopyItemAsync(items[e.Position]);
            var maxHeight = (int)(280 * Resources.DisplayMetrics.Density);
            card.AddView(listView, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, maxHeight));
        }

        SetContentView(card, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
    }

    private LinearLayout.LayoutParams MarginParams(int bottom)
    {
        var p = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        p.SetMargins(0, 0, 0, (int)(bottom * Resources!.DisplayMetrics!.Density));
        return p;
    }

    private static GradientDrawable RoundedCard(Color color, float radiusDp)
    {
        var drawable = new GradientDrawable();
        drawable.SetColor(color);
        drawable.SetCornerRadius(radiusDp);
        return drawable;
    }

    private sealed class HistoryAdapter(Activity context, IReadOnlyList<ClipboardHistoryItem> items, Color rowColor)
        : BaseAdapter<ClipboardHistoryItem>
    {
        public override int Count => items.Count;
        public override ClipboardHistoryItem this[int position] => items[position];
        public override long GetItemId(int position) => position;

        public override View GetView(int position, View? convertView, ViewGroup? parent)
        {
            var row = new TextView(context);
            var pad = (int)(12 * context.Resources!.DisplayMetrics!.Density);
            row.SetPadding(pad, pad, pad, pad);
            row.SetTextColor(Color.White);
            row.Background = RoundedCard(rowColor, radiusDp: 10);
            row.Text = Describe(items[position]);
            return row;
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
