using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Lumora.Server.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository(LumoraDbContext db) : IRoomRepository
{
    public Task<Room?> GetBySlugAsync(RoomSlug slug, CancellationToken ct) =>
        db.Rooms.FirstOrDefaultAsync(r => r.Slug == slug, ct);

    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> SlugExistsAsync(RoomSlug slug, CancellationToken ct) =>
        db.Rooms.AnyAsync(r => r.Slug == slug, ct);

    public async Task<IReadOnlyList<Room>> ListAllAsync(CancellationToken ct) =>
        await db.Rooms.ToListAsync(ct);

    public void Add(Room room) => db.Rooms.Add(room);
}
