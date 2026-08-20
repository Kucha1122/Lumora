using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    Task<Room?> GetBySlugAsync(RoomSlug slug, CancellationToken ct);

    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<bool> SlugExistsAsync(RoomSlug slug, CancellationToken ct);

    /// <summary>Room names and their public/private flag are browsable by anyone —
    /// this is a deliberate directory feature, not a leak: passwords and content stay
    /// protected regardless of who can see that a private room exists.</summary>
    Task<IReadOnlyList<Room>> ListAllAsync(CancellationToken ct);

    void Add(Room room);
}
