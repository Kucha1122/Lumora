using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Crypto;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Transport;
using Lumora.Contracts.Clipboard;
using Lumora.Contracts.Realtime;

namespace Lumora.Client.Core.Sync;

/// <summary>
/// Wires the local OS clipboard to the active room: local changes get encrypted and pushed,
/// remote pushes get decrypted and written back locally. <see cref="LoopGuard"/> stops the
/// two directions from echoing forever. Every entry that passes through here — pushed or
/// received — also lands in <see cref="History"/>, so the history window and quick-paste
/// popup update live instead of polling.
/// </summary>
public sealed class ClipboardSyncEngine(
    IClipboardBridge clipboard,
    LumoraApiClient api,
    LumoraRealtimeClient realtime,
    ActiveRoomStore activeRoom)
{
    private readonly LoopGuard loopGuard = new();
    private Guid ownDeviceId;

    public ClipboardHistoryStore History { get; } = new();

    /// <summary>
    /// Writes a history item back to the OS clipboard without it re-entering History as a
    /// "new" push — re-copying something you already have shouldn't duplicate it in the list.
    /// </summary>
    public async Task PasteFromHistoryAsync(ClipboardHistoryItem item, CancellationToken ct)
    {
        loopGuard.SuppressNext(item.Plaintext);
        await clipboard.SetContentAsync(new LocalClipboardContent(item.Kind, item.Plaintext), ct);
    }

    /// <summary>Same suppression, for callers (e.g. the quick-paste popup) that write to the
    /// clipboard through a different path than <see cref="PasteFromHistoryAsync"/>.</summary>
    public void SuppressNextLocalChange(byte[] plaintext) => loopGuard.SuppressNext(plaintext);

    /// <summary>Deletes one entry — on the server, then (via the realtime echo from other
    /// devices' perspective, and directly here for this device) out of <see cref="History"/>.</summary>
    public async Task DeleteEntryAsync(Guid entryId, CancellationToken ct)
    {
        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        await api.DeleteClipboardEntryAsync(room.RoomId, entryId, ct);
        History.Remove(entryId);
    }

    public async Task ClearHistoryAsync(CancellationToken ct)
    {
        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        await api.ClearClipboardAsync(room.RoomId, ct);
        History.Clear();
    }

    public async Task AttachAsync(Guid deviceId, CancellationToken ct)
    {
        ownDeviceId = deviceId;
        clipboard.ContentChanged += OnLocalContentChangedAsync;
        realtime.ClipboardEntryPushed += OnRemoteEntryPushedAsync;
        realtime.ClipboardEntryDeleted += OnRemoteEntryDeletedAsync;
        realtime.ClipboardCleared += OnRemoteClearedAsync;

        await LoadInitialHistoryAsync(ct);
    }

    public void Detach()
    {
        clipboard.ContentChanged -= OnLocalContentChangedAsync;
        realtime.ClipboardEntryPushed -= OnRemoteEntryPushedAsync;
        realtime.ClipboardEntryDeleted -= OnRemoteEntryDeletedAsync;
        realtime.ClipboardCleared -= OnRemoteClearedAsync;
        History.Clear();
    }

    private Task OnRemoteEntryDeletedAsync(ClipboardEntryDeletedEvent evt)
    {
        History.Remove(evt.EntryId);
        return Task.CompletedTask;
    }

    private Task OnRemoteClearedAsync(ClipboardClearedEvent evt)
    {
        History.Clear();
        return Task.CompletedTask;
    }

    private async Task LoadInitialHistoryAsync(CancellationToken ct)
    {
        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        var entries = await api.ListClipboardEntriesAsync(room.RoomId, ct);
        var items = new List<ClipboardHistoryItem>();

        foreach (var entry in entries)
        {
            if (entry.InlinePayload is null)
            {
                continue; // Blob-backed entries aren't shown in quick history — download on demand instead.
            }

            var plaintext = TryDecrypt(room, entry.InlinePayload);
            if (plaintext is null)
            {
                continue;
            }

            items.Add(new ClipboardHistoryItem(
                entry.Id, ToLocalKind(entry.Kind), plaintext, entry.DeviceId, entry.CreatedAt));
        }

        History.ReplaceAll(items);
    }

    private async Task OnLocalContentChangedAsync(LocalClipboardContent content)
    {
        if (loopGuard.ShouldIgnore(content.Data))
        {
            return;
        }

        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        var payload = room.IsPrivate ? PayloadCipher.Encrypt(room.EncKey!, content.Data) : content.Data;
        var kind = content.Kind == LocalClipboardContentKind.Text
            ? ClipboardEntryKindDto.Text
            : ClipboardEntryKindDto.Image;

        ClipboardEntryDto pushed;
        if (payload.Length <= ClipboardInlineThreshold)
        {
            pushed = await api.PushClipboardEntryAsync(
                room.RoomId,
                new PushClipboardEntryRequest(kind, ownDeviceId, payload, BlobId: null, payload.Length),
                CancellationToken.None);
        }
        else
        {
            using var stream = new MemoryStream(payload);
            var blobId = await api.UploadBlobAsync(room.RoomId, stream, CancellationToken.None);
            pushed = await api.PushClipboardEntryAsync(
                room.RoomId,
                new PushClipboardEntryRequest(kind, ownDeviceId, InlinePayload: null, blobId, payload.Length),
                CancellationToken.None);
        }

        History.Add(new ClipboardHistoryItem(pushed.Id, content.Kind, content.Data, ownDeviceId, pushed.CreatedAt));
    }

    private async Task OnRemoteEntryPushedAsync(ClipboardEntryPushedEvent evt)
    {
        var entry = evt.Entry;
        if (entry.DeviceId == ownDeviceId)
        {
            return;
        }

        var room = activeRoom.ActiveRoom;
        if (room is null)
        {
            return;
        }

        byte[] payload;
        if (entry.InlinePayload is not null)
        {
            payload = entry.InlinePayload;
        }
        else if (entry.BlobId is { } blobId)
        {
            using var stream = await api.DownloadBlobAsync(room.RoomId, blobId, CancellationToken.None);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            payload = buffer.ToArray();
        }
        else
        {
            return;
        }

        var plaintext = room.IsPrivate ? PayloadCipher.Decrypt(room.EncKey!, payload) : payload;

        loopGuard.SuppressNext(plaintext);

        var kind = ToLocalKind(entry.Kind);

        await clipboard.SetContentAsync(new LocalClipboardContent(kind, plaintext), CancellationToken.None);
        History.Add(new ClipboardHistoryItem(entry.Id, kind, plaintext, entry.DeviceId, entry.CreatedAt));
    }

    private static byte[]? TryDecrypt(RoomProfile room, byte[] payload)
    {
        try
        {
            return room.IsPrivate ? PayloadCipher.Decrypt(room.EncKey!, payload) : payload;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    private static LocalClipboardContentKind ToLocalKind(ClipboardEntryKindDto kind) =>
        kind == ClipboardEntryKindDto.Text ? LocalClipboardContentKind.Text : LocalClipboardContentKind.Image;

    /// <summary>Mirrors ClipboardEntry.InlineThresholdBytes on the server — kept in sync manually,
    /// since Client.Core cannot reference Domain (see plan §Struktura rozwiązania).</summary>
    private const int ClipboardInlineThreshold = 256 * 1024;
}
