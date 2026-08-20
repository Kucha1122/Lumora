using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Rooms;

public sealed record JoinRoomCommand(
    string Slug,
    byte[]? AuthKey,
    string DeviceDisplayName,
    string Platform) : IRequest<Result<JoinRoomResult>>;

public sealed record JoinRoomResult(
    Guid RoomId, string Slug, string DisplayName, bool IsPrivate, string AccessToken, Guid DeviceId);
