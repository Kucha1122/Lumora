using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lumora.Contracts.Clipboard;
using Lumora.Contracts.Drive;
using Lumora.Contracts.Rooms;

namespace Lumora.Client.Core.Transport;

/// <summary>Thin REST client. Never encrypts or decrypts — callers pass/receive raw bytes.</summary>
public sealed class LumoraApiClient(HttpClient http)
{
    /// <summary>Every endpoint except CreateRoom/GetRoomSalt/JoinRoom is room-scoped and requires
    /// this — call it with the token from JoinRoomAsync before using any other method.</summary>
    public void SetAccessToken(string? accessToken) =>
        http.DefaultRequestHeaders.Authorization = accessToken is null
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);

    public async Task<IReadOnlyList<RoomSummaryDto>> ListRoomsAsync(CancellationToken ct)
    {
        var rooms = await http.GetFromJsonAsync<List<RoomSummaryDto>>("/rooms", ct);
        return rooms ?? [];
    }

    public async Task<CreateRoomResponse> CreateRoomAsync(CreateRoomRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync("/rooms", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateRoomResponse>(ct))!;
    }

    public async Task<byte[]> GetRoomSaltAsync(string slug, CancellationToken ct)
    {
        var response = await http.GetFromJsonAsync<GetRoomSaltResponse>($"/rooms/{slug}/salt", ct);
        return response!.KdfSalt;
    }

    /// <summary>Returns null on 401 — an unknown slug and a wrong password look identical. See JoinRoomHandler.</summary>
    public async Task<JoinRoomResponse?> JoinRoomAsync(string slug, JoinRoomRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync($"/rooms/{slug}/join", request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JoinRoomResponse>(ct);
    }

    public async Task<IReadOnlyList<ClipboardEntryDto>> ListClipboardEntriesAsync(Guid roomId, CancellationToken ct)
    {
        var entries = await http.GetFromJsonAsync<List<ClipboardEntryDto>>($"/rooms/{roomId}/clipboard", ct);
        return entries ?? [];
    }

    public async Task<ClipboardEntryDto> PushClipboardEntryAsync(Guid roomId, PushClipboardEntryRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync($"/rooms/{roomId}/clipboard", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClipboardEntryDto>(ct))!;
    }

    public async Task DeleteClipboardEntryAsync(Guid roomId, Guid entryId, CancellationToken ct)
    {
        var response = await http.DeleteAsync($"/rooms/{roomId}/clipboard/{entryId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ClearClipboardAsync(Guid roomId, CancellationToken ct)
    {
        var response = await http.DeleteAsync($"/rooms/{roomId}/clipboard", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<DriveFileDto>> ListDriveFilesAsync(Guid roomId, CancellationToken ct)
    {
        var files = await http.GetFromJsonAsync<List<DriveFileDto>>($"/rooms/{roomId}/drive", ct);
        return files ?? [];
    }

    public async Task<Guid> UploadBlobAsync(Guid roomId, Stream content, CancellationToken ct)
    {
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new("application/octet-stream");
        var response = await http.PostAsync($"/rooms/{roomId}/drive/blobs", streamContent, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(ct);
    }

    public async Task<Stream> DownloadBlobAsync(Guid roomId, Guid blobId, CancellationToken ct)
    {
        var response = await http.GetAsync($"/rooms/{roomId}/drive/blobs/{blobId}", HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task RegisterDriveFileAsync(Guid roomId, RegisterDriveFileRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync($"/rooms/{roomId}/drive", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveFileAsync(Guid roomId, Guid fileId, CancellationToken ct)
    {
        var response = await http.DeleteAsync($"/rooms/{roomId}/drive/{fileId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
