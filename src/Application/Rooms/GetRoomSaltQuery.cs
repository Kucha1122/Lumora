using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.ValueObjects;
using MediatR;

namespace Lumora.Server.Application.Rooms;

/// <summary>
/// Always returns a salt, whether or not the room exists — see <see cref="IRoomAuth.DeriveFakeSalt"/>.
/// The client cannot distinguish "unknown slug" from "real private room" from this response alone.
/// </summary>
public sealed record GetRoomSaltQuery(string Slug) : IRequest<byte[]>;

public sealed class GetRoomSaltHandler(IRoomRepository rooms, IRoomAuth roomAuth)
    : IRequestHandler<GetRoomSaltQuery, byte[]>
{
    public async Task<byte[]> Handle(GetRoomSaltQuery request, CancellationToken ct)
    {
        var slugResult = RoomSlug.Create(request.Slug);
        if (slugResult.IsSuccess)
        {
            var room = await rooms.GetBySlugAsync(slugResult.Value!, ct);
            if (room is { KdfSalt: not null })
            {
                return room.KdfSalt;
            }
        }

        return roomAuth.DeriveFakeSalt(request.Slug);
    }
}
