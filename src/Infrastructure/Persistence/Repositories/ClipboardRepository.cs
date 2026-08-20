using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Clipboard;
using Microsoft.EntityFrameworkCore;

namespace Lumora.Server.Infrastructure.Persistence.Repositories;

public sealed class ClipboardRepository(LumoraDbContext db) : IClipboardRepository
{
    public async Task<IReadOnlyList<ClipboardEntry>> ListRecentAsync(Guid roomId, int take, CancellationToken ct) =>
        await db.ClipboardEntries
            .Where(e => e.RoomId == roomId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClipboardEntry>> ListAllAsync(Guid roomId, CancellationToken ct) =>
        await db.ClipboardEntries
            .Where(e => e.RoomId == roomId)
            .ToListAsync(ct);

    public Task<ClipboardEntry?> GetByIdAsync(Guid roomId, Guid entryId, CancellationToken ct) =>
        db.ClipboardEntries.FirstOrDefaultAsync(e => e.RoomId == roomId && e.Id == entryId, ct);

    public async Task<IReadOnlyList<ClipboardEntry>> ListOverflowAsync(
        Guid roomId, int maxCount, TimeSpan maxAge, DateTimeOffset now, CancellationToken ct)
    {
        var cutoff = now - maxAge;

        var ordered = await db.ClipboardEntries
            .Where(e => e.RoomId == roomId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        var beyondMaxCount = ordered.Skip(maxCount);
        var expired = ordered.Where(e => e.CreatedAt < cutoff);

        return beyondMaxCount.Union(expired).ToList();
    }

    public void Add(ClipboardEntry entry) => db.ClipboardEntries.Add(entry);

    public void Remove(ClipboardEntry entry) => db.ClipboardEntries.Remove(entry);
}
