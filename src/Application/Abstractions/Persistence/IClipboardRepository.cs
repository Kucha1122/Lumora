using Lumora.Server.Domain.Clipboard;

namespace Lumora.Server.Application.Abstractions.Persistence;

public interface IClipboardRepository
{
    Task<IReadOnlyList<ClipboardEntry>> ListRecentAsync(Guid roomId, int take, CancellationToken ct);

    Task<IReadOnlyList<ClipboardEntry>> ListAllAsync(Guid roomId, CancellationToken ct);

    Task<ClipboardEntry?> GetByIdAsync(Guid roomId, Guid entryId, CancellationToken ct);

    /// <summary>Entries older than the retention window or beyond the max count for the room.</summary>
    Task<IReadOnlyList<ClipboardEntry>> ListOverflowAsync(
        Guid roomId, int maxCount, TimeSpan maxAge, DateTimeOffset now, CancellationToken ct);

    void Add(ClipboardEntry entry);

    void Remove(ClipboardEntry entry);
}
