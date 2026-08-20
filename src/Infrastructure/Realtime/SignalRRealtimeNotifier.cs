using Lumora.Contracts.Clipboard;
using Lumora.Contracts.Drive;
using Lumora.Contracts.Realtime;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.Drive;
using Microsoft.AspNetCore.SignalR;

namespace Lumora.Server.Infrastructure.Realtime;

public sealed class SignalRRealtimeNotifier(IHubContext<ClipboardHub> hub) : IRealtimeNotifier
{
    public Task ClipboardEntryPushedAsync(Guid roomId, ClipboardEntry entry, CancellationToken ct) =>
        hub.Clients.Group(ClipboardHub.GroupName(roomId)).SendAsync(
            RealtimeHubMethods.ClipboardEntryPushed, new ClipboardEntryPushedEvent(ToDto(entry)), ct);

    public Task DriveFileAddedAsync(Guid roomId, DriveFile file, CancellationToken ct) =>
        hub.Clients.Group(ClipboardHub.GroupName(roomId)).SendAsync(
            RealtimeHubMethods.DriveFileAdded, new DriveFileAddedEvent(ToDto(file)), ct);

    public Task DriveFileDeletedAsync(Guid roomId, Guid fileId, CancellationToken ct) =>
        hub.Clients.Group(ClipboardHub.GroupName(roomId)).SendAsync(
            RealtimeHubMethods.DriveFileDeleted, new DriveFileDeletedEvent(fileId), ct);

    public Task ClipboardEntryDeletedAsync(Guid roomId, Guid entryId, CancellationToken ct) =>
        hub.Clients.Group(ClipboardHub.GroupName(roomId)).SendAsync(
            RealtimeHubMethods.ClipboardEntryDeleted, new ClipboardEntryDeletedEvent(entryId), ct);

    public Task ClipboardClearedAsync(Guid roomId, CancellationToken ct) =>
        hub.Clients.Group(ClipboardHub.GroupName(roomId)).SendAsync(
            RealtimeHubMethods.ClipboardCleared, new ClipboardClearedEvent(), ct);

    private static ClipboardEntryDto ToDto(ClipboardEntry entry) => new(
        entry.Id,
        (ClipboardEntryKindDto)entry.Kind,
        entry.InlinePayload?.Bytes,
        entry.BlobId is null ? null : Guid.Parse(entry.BlobId.ToString()),
        entry.SizeBytes,
        entry.DeviceId,
        entry.CreatedAt);

    private static DriveFileDto ToDto(DriveFile file) => new(
        file.Id,
        file.EncryptedMetadata.Bytes,
        Guid.Parse(file.BlobId.ToString()),
        file.SizeBytes,
        file.DeviceId,
        file.CreatedAt);
}
