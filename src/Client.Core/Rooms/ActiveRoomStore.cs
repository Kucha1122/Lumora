namespace Lumora.Client.Core.Rooms;

/// <summary>
/// Tracks which room is currently active — the client is always in exactly one room at a
/// time, and it's this room that receives copied clipboard content and shows its Drive files.
/// Defaults to the public room on first run, with no prompt: see plan §Wybór przestrzeni.
/// </summary>
public sealed class ActiveRoomStore(ISecureStorage secureStorage)
{
    public const string PublicSlug = "public";

    private readonly List<RoomProfile> knownRooms = [];

    public RoomProfile? ActiveRoom { get; private set; }

    public IReadOnlyList<RoomProfile> KnownRooms => knownRooms;

    public event Action<RoomProfile?>? ActiveRoomChanged;

    public async Task LoadAsync(CancellationToken ct)
    {
        knownRooms.Clear();
        knownRooms.AddRange(await secureStorage.LoadRoomsAsync(ct));

        var activeId = await secureStorage.LoadActiveRoomIdAsync(ct);
        ActiveRoom = activeId is { } id ? knownRooms.FirstOrDefault(r => r.RoomId == id) : null;
    }

    public async Task RememberAsync(RoomProfile profile, CancellationToken ct)
    {
        knownRooms.RemoveAll(r => r.RoomId == profile.RoomId);
        knownRooms.Add(profile);
        await secureStorage.SaveRoomAsync(profile, ct);
    }

    public async Task SwitchToAsync(RoomProfile profile, CancellationToken ct)
    {
        await RememberAsync(profile, ct);
        ActiveRoom = profile;
        await secureStorage.SaveActiveRoomIdAsync(profile.RoomId, ct);
        ActiveRoomChanged?.Invoke(profile);
    }
}
