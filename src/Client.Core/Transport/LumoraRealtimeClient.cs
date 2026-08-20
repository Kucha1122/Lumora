using Lumora.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

namespace Lumora.Client.Core.Transport;

/// <summary>
/// SignalR connection scoped to a single room via its access token. Auto-reconnects with
/// backoff; the hub itself re-joins the caller to its room group server-side on (re)connect
/// based on the token's "roomId" claim — see ClipboardHub.OnConnectedAsync.
/// </summary>
public sealed class LumoraRealtimeClient : IAsyncDisposable
{
    private HubConnection? connection;

    public event Func<ClipboardEntryPushedEvent, Task>? ClipboardEntryPushed;
    public event Func<ClipboardEntryDeletedEvent, Task>? ClipboardEntryDeleted;
    public event Func<ClipboardClearedEvent, Task>? ClipboardCleared;
    public event Func<DriveFileAddedEvent, Task>? DriveFileAdded;
    public event Func<DriveFileDeletedEvent, Task>? DriveFileDeleted;

    public async Task ConnectAsync(Uri hubUri, string accessToken, CancellationToken ct)
    {
        await DisposeConnectionAsync();

        connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options => options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken))
            .WithAutomaticReconnect()
            .Build();

        connection.On<ClipboardEntryPushedEvent>(
            RealtimeHubMethods.ClipboardEntryPushed, e => ClipboardEntryPushed?.Invoke(e) ?? Task.CompletedTask);
        connection.On<ClipboardEntryDeletedEvent>(
            RealtimeHubMethods.ClipboardEntryDeleted, e => ClipboardEntryDeleted?.Invoke(e) ?? Task.CompletedTask);
        connection.On<ClipboardClearedEvent>(
            RealtimeHubMethods.ClipboardCleared, e => ClipboardCleared?.Invoke(e) ?? Task.CompletedTask);
        connection.On<DriveFileAddedEvent>(
            RealtimeHubMethods.DriveFileAdded, e => DriveFileAdded?.Invoke(e) ?? Task.CompletedTask);
        connection.On<DriveFileDeletedEvent>(
            RealtimeHubMethods.DriveFileDeleted, e => DriveFileDeleted?.Invoke(e) ?? Task.CompletedTask);

        await connection.StartAsync(ct);
    }

    public async ValueTask DisposeAsync() => await DisposeConnectionAsync();

    private async Task DisposeConnectionAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
            connection = null;
        }
    }
}
