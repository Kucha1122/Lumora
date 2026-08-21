using System.Windows.Input;
using Lumora.Client.Android.Clipboard;
using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Sync;

namespace Lumora.Client.Android.Pages;

public sealed record ClipboardRow(string Preview, string SubText, ICommand CopyCommand, ICommand DeleteCommand);

public partial class ClipboardPage : ContentPage
{
    private readonly ClipboardSyncEngine syncEngine;
    private readonly AndroidClipboardBridge clipboardBridge;
    private readonly ActiveRoomStore activeRoom;

    public ClipboardPage(ClipboardSyncEngine syncEngine, AndroidClipboardBridge clipboardBridge, ActiveRoomStore activeRoom)
    {
        InitializeComponent();
        this.syncEngine = syncEngine;
        this.clipboardBridge = clipboardBridge;
        this.activeRoom = activeRoom;

        syncEngine.History.Changed += OnHistoryChanged;
        activeRoom.ActiveRoomChanged += _ => MainThread.BeginInvokeOnMainThread(UpdateRoomLabel);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateRoomLabel();
        RefreshList();
    }

    private void OnHistoryChanged() => MainThread.BeginInvokeOnMainThread(RefreshList);

    private void UpdateRoomLabel()
    {
        var room = activeRoom.ActiveRoom;
        RoomLabel.Text = room is null
            ? "Brak aktywnej przestrzeni"
            : $"Przestrzeń: {room.DisplayName}{(room.IsPrivate ? " 🔒" : "")}";
    }

    private void RefreshList()
    {
        HistoryList.ItemsSource = syncEngine.History.Items.Select(item => new ClipboardRow(
            Preview: item.Kind == LocalClipboardContentKind.Text
                ? System.Text.Encoding.UTF8.GetString(item.Plaintext)
                : $"[obraz, {item.Plaintext.Length} B]",
            SubText: item.CreatedAt.LocalDateTime.ToString("g"),
            CopyCommand: new Command(async () => await CopyToClipboardAsync(item)),
            DeleteCommand: new Command(async () => await syncEngine.DeleteEntryAsync(item.EntryId, CancellationToken.None))
        )).ToList();
    }

    private async Task CopyToClipboardAsync(ClipboardHistoryItem item)
    {
        await syncEngine.PasteFromHistoryAsync(item, CancellationToken.None);
        await DisplayAlert(null, "Skopiowano do schowka.", "OK");
    }

    private async void OnSendClipboardClicked(object? sender, EventArgs e)
    {
        var content = clipboardBridge.TryCaptureCurrentClipboard();
        if (content is null)
        {
            await DisplayAlert(null, "Schowek telefonu jest pusty.", "OK");
            return;
        }

        await clipboardBridge.RaiseContentChangedAsync(content);
    }

    private async void OnClearClicked(object? sender, EventArgs e) =>
        await syncEngine.ClearHistoryAsync(CancellationToken.None);
}
