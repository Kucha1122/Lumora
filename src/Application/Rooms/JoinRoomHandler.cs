using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.Devices;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using MediatR;

namespace Lumora.Server.Application.Rooms;

/// <summary>
/// Returns the exact same failure for "no such room" and "wrong password" — never let a
/// slug's existence leak through this endpoint. See CLAUDE plan §Model dostępu.
/// </summary>
public sealed class JoinRoomHandler(
    IRoomRepository rooms,
    IDeviceRepository devices,
    IUnitOfWork unitOfWork,
    IRoomAuth roomAuth,
    IDateTimeProvider clock) : IRequestHandler<JoinRoomCommand, Result<JoinRoomResult>>
{
    private const string GenericFailure = "Nieprawidłowa przestrzeń lub hasło.";

    public async Task<Result<JoinRoomResult>> Handle(JoinRoomCommand request, CancellationToken ct)
    {
        var slugResult = RoomSlug.Create(request.Slug);
        var room = slugResult.IsSuccess ? await rooms.GetBySlugAsync(slugResult.Value!, ct) : null;

        if (room is null)
        {
            // Burn the same cryptographic work a real private-room join would, so an
            // attacker can't distinguish "unknown slug" from "wrong password" by timing.
            if (request.AuthKey is { Length: > 0 })
            {
                roomAuth.VerifyAuthKey(request.AuthKey, roomAuth.DeriveFakeAuthKeyHash(request.Slug));
            }

            return Result<JoinRoomResult>.Failure(GenericFailure);
        }

        if (room.Visibility == RoomVisibility.Private)
        {
            if (request.AuthKey is not { Length: > 0 })
            {
                return Result<JoinRoomResult>.Failure(GenericFailure);
            }

            var isValid = roomAuth.VerifyAuthKey(request.AuthKey, room.AuthKeyHash!);
            if (!isValid)
            {
                return Result<JoinRoomResult>.Failure(GenericFailure);
            }
        }

        var deviceResult = Device.Register(room.Id, request.DeviceDisplayName, request.Platform, clock.UtcNow);
        if (!deviceResult.IsSuccess)
        {
            return Result<JoinRoomResult>.Failure(deviceResult.Error!);
        }

        var device = deviceResult.Value!;
        devices.Add(device);
        await unitOfWork.SaveChangesAsync(ct);

        var token = roomAuth.IssueAccessToken(room.Id, device.Id);

        return Result<JoinRoomResult>.Success(new JoinRoomResult(
            room.Id, room.Slug.Value, room.DisplayName, room.Visibility == RoomVisibility.Private, token, device.Id));
    }
}
