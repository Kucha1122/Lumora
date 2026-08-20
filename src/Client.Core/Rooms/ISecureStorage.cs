namespace Lumora.Client.Core.Rooms;

/// <summary>
/// Persists known-room key material across restarts using the OS credential vault
/// (DPAPI on Windows, Keychain on macOS). Implemented per-platform in the client shell.
/// </summary>
public interface ISecureStorage
{
    Task SaveRoomAsync(RoomProfile profile, CancellationToken ct);

    Task<IReadOnlyList<RoomProfile>> LoadRoomsAsync(CancellationToken ct);

    Task RemoveRoomAsync(Guid roomId, CancellationToken ct);

    Task SaveActiveRoomIdAsync(Guid? roomId, CancellationToken ct);

    Task<Guid?> LoadActiveRoomIdAsync(CancellationToken ct);
}
