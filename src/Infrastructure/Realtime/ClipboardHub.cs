using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lumora.Server.Infrastructure.Realtime;

/// <summary>
/// Pure relay — a connecting client is auto-joined to its room's group based on the
/// "roomId" claim in its JWT (the same token used for REST), so it can never join a
/// group for a room it hasn't authenticated into. No business logic lives here; pushes
/// are triggered by <see cref="SignalRRealtimeNotifier"/> from Application handlers.
/// </summary>
[Authorize]
public sealed class ClipboardHub : Hub
{
    public const string Route = "/hubs/room";

    public static string GroupName(Guid roomId) => $"room:{roomId}";

    public override async Task OnConnectedAsync()
    {
        var roomId = GetRoomId();
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(roomId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var roomId = GetRoomId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(roomId));
        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetRoomId()
    {
        var claim = Context.User?.FindFirst("roomId")?.Value;
        return Guid.TryParse(claim, out var roomId) ? roomId : Guid.Empty;
    }
}
