using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Lumora.Client.Core.Rooms;

namespace Lumora.Client.Desktop.Rooms;

/// <summary>
/// Persists known rooms (including their EncKey, for private rooms) to a single file
/// under %AppData%, encrypted at rest with DPAPI so only this Windows user account can
/// read it back — see plan §Client.Core: "encKey w ISecureStorage, na Windows przez DPAPI".
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecureStorage : ISecureStorage
{
    private readonly string filePath;
    private static readonly byte[] Entropy = "Lumora.Rooms.v1"u8.ToArray();

    public DpapiSecureStorage()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumora");
        Directory.CreateDirectory(dir);
        filePath = Path.Combine(dir, "rooms.dat");
    }

    public async Task SaveRoomAsync(RoomProfile profile, CancellationToken ct)
    {
        var state = await LoadStateAsync(ct);
        state.Rooms.RemoveAll(r => r.RoomId == profile.RoomId);
        state.Rooms.Add(profile);
        await SaveStateAsync(state, ct);
    }

    public async Task<IReadOnlyList<RoomProfile>> LoadRoomsAsync(CancellationToken ct) =>
        (await LoadStateAsync(ct)).Rooms;

    public async Task RemoveRoomAsync(Guid roomId, CancellationToken ct)
    {
        var state = await LoadStateAsync(ct);
        state.Rooms.RemoveAll(r => r.RoomId == roomId);
        await SaveStateAsync(state, ct);
    }

    public async Task SaveActiveRoomIdAsync(Guid? roomId, CancellationToken ct)
    {
        var state = await LoadStateAsync(ct);
        state.ActiveRoomId = roomId;
        await SaveStateAsync(state, ct);
    }

    public async Task<Guid?> LoadActiveRoomIdAsync(CancellationToken ct) =>
        (await LoadStateAsync(ct)).ActiveRoomId;

    private async Task<PersistedState> LoadStateAsync(CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            return new PersistedState();
        }

        var encrypted = await File.ReadAllBytesAsync(filePath, ct);
        var json = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<PersistedState>(json) ?? new PersistedState();
    }

    private async Task SaveStateAsync(PersistedState state, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(state);
        var encrypted = ProtectedData.Protect(json, Entropy, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(filePath, encrypted, ct);
    }

    private sealed class PersistedState
    {
        public List<RoomProfile> Rooms { get; set; } = [];
        public Guid? ActiveRoomId { get; set; }
    }
}
