using System.Text.Json;
using Lumora.Client.Core.Rooms;
using Microsoft.Maui.Storage;

namespace Lumora.Client.Android.Rooms;

/// <summary>
/// Persists known rooms (including EncKey for private rooms) as a single JSON blob under
/// Microsoft.Maui.Storage.SecureStorage, which on Android is backed by EncryptedSharedPreferences
/// + the Android Keystore — the Android equivalent of DpapiSecureStorage on Windows.
/// </summary>
public sealed class MauiSecureStorage : Lumora.Client.Core.Rooms.ISecureStorage
{
    private const string Key = "lumora-rooms-state";

    public async Task SaveRoomAsync(RoomProfile profile, CancellationToken ct)
    {
        var state = await LoadStateAsync();
        state.Rooms.RemoveAll(r => r.RoomId == profile.RoomId);
        state.Rooms.Add(profile);
        await SaveStateAsync(state);
    }

    public async Task<IReadOnlyList<RoomProfile>> LoadRoomsAsync(CancellationToken ct) =>
        (await LoadStateAsync()).Rooms;

    public async Task RemoveRoomAsync(Guid roomId, CancellationToken ct)
    {
        var state = await LoadStateAsync();
        state.Rooms.RemoveAll(r => r.RoomId == roomId);
        await SaveStateAsync(state);
    }

    public async Task SaveActiveRoomIdAsync(Guid? roomId, CancellationToken ct)
    {
        var state = await LoadStateAsync();
        state.ActiveRoomId = roomId;
        await SaveStateAsync(state);
    }

    public async Task<Guid?> LoadActiveRoomIdAsync(CancellationToken ct) =>
        (await LoadStateAsync()).ActiveRoomId;

    private static async Task<PersistedState> LoadStateAsync()
    {
        var json = await SecureStorage.Default.GetAsync(Key);
        if (string.IsNullOrEmpty(json))
        {
            return new PersistedState();
        }

        return JsonSerializer.Deserialize<PersistedState>(json) ?? new PersistedState();
    }

    private static Task SaveStateAsync(PersistedState state) =>
        SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(state));

    private sealed class PersistedState
    {
        public List<RoomProfile> Rooms { get; set; } = [];
        public Guid? ActiveRoomId { get; set; }
    }
}
