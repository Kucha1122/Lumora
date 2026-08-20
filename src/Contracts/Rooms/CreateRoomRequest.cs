namespace Lumora.Contracts.Rooms;

/// <summary>
/// KdfSalt/AuthKey are generated and derived client-side from the room password.
/// The server never sees the password itself.
/// </summary>
public sealed record CreateRoomRequest(
    string Slug,
    string DisplayName,
    bool IsPrivate,
    byte[]? KdfSalt,
    byte[]? AuthKey);

public sealed record CreateRoomResponse(Guid RoomId, string Slug, bool IsPrivate);

/// <summary>
/// Returns a KDF salt for the given slug whether or not the room exists — for an
/// unknown slug the salt is deterministically derived from a server-side pepper so the
/// response is indistinguishable from a real room. This prevents the salt lookup itself
/// from being an existence oracle for private room slugs.
/// </summary>
public sealed record GetRoomSaltResponse(byte[] KdfSalt);

public sealed record JoinRoomRequest(byte[]? AuthKey, string DeviceDisplayName, string Platform);

public sealed record JoinRoomResponse(
    Guid RoomId, string Slug, string DisplayName, bool IsPrivate, string AccessToken, Guid DeviceId);
