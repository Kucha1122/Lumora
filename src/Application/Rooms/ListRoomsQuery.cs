using Lumora.Server.Application.Abstractions.Persistence;
using MediatR;

namespace Lumora.Server.Application.Rooms;

public sealed record RoomSummary(string Slug, string DisplayName, bool IsPrivate);

/// <summary>No auth required — room names and public/private status are a browsable
/// directory, not a secret. See IRoomRepository.ListAllAsync.</summary>
public sealed record ListRoomsQuery : IRequest<IReadOnlyList<RoomSummary>>;

public sealed class ListRoomsHandler(IRoomRepository rooms) : IRequestHandler<ListRoomsQuery, IReadOnlyList<RoomSummary>>
{
    public async Task<IReadOnlyList<RoomSummary>> Handle(ListRoomsQuery request, CancellationToken ct)
    {
        var all = await rooms.ListAllAsync(ct);
        return all
            .OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(r => new RoomSummary(r.Slug.Value, r.DisplayName, r.Visibility == Domain.Rooms.RoomVisibility.Private))
            .ToList();
    }
}
