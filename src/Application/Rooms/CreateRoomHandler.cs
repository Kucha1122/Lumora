using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using MediatR;

namespace Lumora.Server.Application.Rooms;

public sealed class CreateRoomHandler(
    IRoomRepository rooms,
    IUnitOfWork unitOfWork,
    IRoomAuth roomAuth,
    IDateTimeProvider clock) : IRequestHandler<CreateRoomCommand, Result<CreateRoomResult>>
{
    public async Task<Result<CreateRoomResult>> Handle(CreateRoomCommand request, CancellationToken ct)
    {
        var slugResult = RoomSlug.Create(request.Slug);
        if (!slugResult.IsSuccess)
        {
            return Result<CreateRoomResult>.Failure(slugResult.Error!);
        }

        var slug = slugResult.Value!;

        if (await rooms.SlugExistsAsync(slug, ct))
        {
            return Result<CreateRoomResult>.Failure("Przestrzeń o tym slugu już istnieje.");
        }

        Result<Room> roomResult;

        if (request.IsPrivate)
        {
            if (request.KdfSalt is not { Length: > 0 } || request.AuthKey is not { Length: > 0 })
            {
                return Result<CreateRoomResult>.Failure(
                    "Prywatna przestrzeń wymaga soli KDF i klucza uwierzytelniającego.");
            }

            var authKeyHash = roomAuth.HashAuthKey(request.AuthKey);
            roomResult = Room.CreatePrivate(slug, request.DisplayName, request.KdfSalt, authKeyHash, clock.UtcNow);
        }
        else
        {
            roomResult = Room.CreatePublic(slug, request.DisplayName, clock.UtcNow);
        }

        if (!roomResult.IsSuccess)
        {
            return Result<CreateRoomResult>.Failure(roomResult.Error!);
        }

        var room = roomResult.Value!;
        rooms.Add(room);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<CreateRoomResult>.Success(
            new CreateRoomResult(room.Id, room.Slug.Value, room.Visibility == RoomVisibility.Private));
    }
}
