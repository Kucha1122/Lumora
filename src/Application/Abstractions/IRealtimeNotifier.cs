using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.Drive;

namespace Lumora.Server.Application.Abstractions;

/// <summary>Abstracts SignalR away from Application handlers.</summary>
public interface IRealtimeNotifier
{
    Task ClipboardEntryPushedAsync(Guid roomId, ClipboardEntry entry, CancellationToken ct);

    Task DriveFileAddedAsync(Guid roomId, DriveFile file, CancellationToken ct);

    Task DriveFileDeletedAsync(Guid roomId, Guid fileId, CancellationToken ct);

    Task ClipboardEntryDeletedAsync(Guid roomId, Guid entryId, CancellationToken ct);

    Task ClipboardClearedAsync(Guid roomId, CancellationToken ct);
}
