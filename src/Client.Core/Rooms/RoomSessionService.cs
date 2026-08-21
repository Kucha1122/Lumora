using Lumora.Client.Core.Crypto;
using Lumora.Client.Core.Sync;
using Lumora.Client.Core.Transport;
using Lumora.Contracts.Rooms;

namespace Lumora.Client.Core.Rooms;

/// <summary>
/// Orchestrates joining/switching rooms: derives keys from a password, calls the server,
/// remembers the result, and (re)connects realtime + the clipboard sync engine to whichever
/// room is now active. See plan §Wybór przestrzeni.
/// </summary>
public sealed class RoomSessionService(
    LumoraApiClient api,
    LumoraRealtimeClient realtime,
    ActiveRoomStore activeRoomStore,
    ClipboardSyncEngine clipboardSync,
    IDeviceIdentity deviceIdentity,
    Uri hubUri)
{
    public event Action<RoomProfile?>? ActiveRoomChanged
    {
        add => activeRoomStore.ActiveRoomChanged += value;
        remove => activeRoomStore.ActiveRoomChanged -= value;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await activeRoomStore.LoadAsync(ct);

        if (activeRoomStore.ActiveRoom is not null)
        {
            await ReconnectAsync(ct);
            return;
        }

        // First run: join the public room without asking anything. See plan §Wybór przestrzeni.
        await JoinAsync(ActiveRoomStore.PublicSlug, password: null, ct);
    }

    /// <summary>Creates a room (public or password-protected) then immediately joins it.
    /// A fresh KdfSalt is generated client-side for private rooms — the server never sees
    /// the password, only the derived authKey.</summary>
    public async Task<string?> CreateAsync(string slug, string displayName, bool isPrivate, string? password, CancellationToken ct)
    {
        byte[]? kdfSalt = null;
        byte[]? authKey = null;

        if (isPrivate)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "Prywatna przestrzeń wymaga hasła.";
            }

            kdfSalt = RoomKeyDerivation.GenerateSalt();
            authKey = RoomKeyDerivation.Derive(password, kdfSalt).AuthKey;
        }

        try
        {
            await api.CreateRoomAsync(new CreateRoomRequest(slug, displayName, isPrivate, kdfSalt, authKey), ct);
        }
        catch (HttpRequestException)
        {
            return "Nie udało się utworzyć przestrzeni — slug może być już zajęty.";
        }

        return await JoinAsync(slug, password, ct);
    }

    public async Task<string?> JoinAsync(string slug, string? password, CancellationToken ct)
    {
        byte[]? authKey = null;
        byte[]? encKey = null;

        if (password is not null)
        {
            var salt = await api.GetRoomSaltAsync(slug, ct);
            var keys = RoomKeyDerivation.Derive(password, salt);
            authKey = keys.AuthKey;
            encKey = keys.EncKey;
        }

        var response = await api.JoinRoomAsync(
            slug, new JoinRoomRequest(authKey, deviceIdentity.DisplayName, deviceIdentity.Platform), ct);

        if (response is null)
        {
            return "Nieprawidłowa przestrzeń lub hasło.";
        }

        await ApplyJoinResponseAsync(response, encKey, authKey, ct);
        return null;
    }

    public async Task SwitchToKnownAsync(RoomProfile profile, CancellationToken ct)
    {
        await activeRoomStore.SwitchToAsync(profile, ct);
        await ReconnectAsync(ct);
    }

    private async Task ReconnectAsync(CancellationToken ct)
    {
        var room = activeRoomStore.ActiveRoom!;

        var response = await api.JoinRoomAsync(
            room.Slug, new JoinRoomRequest(room.AuthKey, deviceIdentity.DisplayName, deviceIdentity.Platform), ct);

        if (response is null)
        {
            return;
        }

        await ApplyJoinResponseAsync(response, room.EncKey, room.AuthKey, ct);
    }

    /// <summary>
    /// Always trusts the server's RoomId/DisplayName/IsPrivate from a fresh join response
    /// rather than whatever was previously cached — a room can be recreated server-side
    /// (e.g. a database reset) with a new RoomId under the same slug, and a stale cached
    /// RoomId here would make every subsequent call 403 (its route roomId no longer matches
    /// the fresh token's roomId claim).
    /// </summary>
    private async Task ApplyJoinResponseAsync(JoinRoomResponse response, byte[]? encKey, byte[]? authKey, CancellationToken ct)
    {
        var profile = new RoomProfile(response.RoomId, response.Slug, response.DisplayName, response.IsPrivate, encKey, authKey);
        await activeRoomStore.SwitchToAsync(profile, ct);

        api.SetAccessToken(response.AccessToken);
        await realtime.ConnectAsync(hubUri, response.AccessToken, ct);
        clipboardSync.Detach();
        await clipboardSync.AttachAsync(deviceIdentity.Id, ct);
    }
}
