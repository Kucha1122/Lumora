using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Rooms;

public sealed record CreateRoomCommand(
    string Slug,
    string DisplayName,
    bool IsPrivate,
    byte[]? KdfSalt,
    byte[]? AuthKey) : IRequest<Result<CreateRoomResult>>;

public sealed record CreateRoomResult(Guid RoomId, string Slug, bool IsPrivate);
