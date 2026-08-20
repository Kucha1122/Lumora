using Lumora.Contracts.Rooms;
using Lumora.Server.Application.Rooms;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lumora.Server.Api.Endpoints;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms").WithTags("Rooms");

        group.MapGet("/", ListRooms);
        group.MapPost("/", CreateRoom);
        group.MapGet("/{slug}/salt", GetRoomSalt);
        group.MapPost("/{slug}/join", JoinRoom);
    }

    /// <summary>No auth — see ListRoomsQuery for why this is a deliberate public directory.</summary>
    private static async Task<Ok<IReadOnlyList<RoomSummaryDto>>> ListRooms(ISender sender, CancellationToken ct)
    {
        var rooms = await sender.Send(new ListRoomsQuery(), ct);
        return TypedResults.Ok<IReadOnlyList<RoomSummaryDto>>(
            rooms.Select(r => new RoomSummaryDto(r.Slug, r.DisplayName, r.IsPrivate)).ToList());
    }

    private static async Task<Results<Ok<CreateRoomResponse>, ValidationProblem>> CreateRoom(
        CreateRoomRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateRoomCommand(request.Slug, request.DisplayName, request.IsPrivate, request.KdfSalt, request.AuthKey),
            ct);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] });
        }

        var value = result.Value!;
        return TypedResults.Ok(new CreateRoomResponse(value.RoomId, value.Slug, value.IsPrivate));
    }

    private static async Task<Ok<GetRoomSaltResponse>> GetRoomSalt(string slug, ISender sender, CancellationToken ct)
    {
        var salt = await sender.Send(new GetRoomSaltQuery(slug), ct);
        return TypedResults.Ok(new GetRoomSaltResponse(salt));
    }

    private static async Task<Results<Ok<JoinRoomResponse>, UnauthorizedHttpResult>> JoinRoom(
        string slug, JoinRoomRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new JoinRoomCommand(slug, request.AuthKey, request.DeviceDisplayName, request.Platform), ct);

        if (!result.IsSuccess)
        {
            // Same response shape for "unknown slug" and "wrong password" — see JoinRoomHandler.
            return TypedResults.Unauthorized();
        }

        var value = result.Value!;
        return TypedResults.Ok(new JoinRoomResponse(
            value.RoomId, value.Slug, value.DisplayName, value.IsPrivate, value.AccessToken, value.DeviceId));
    }
}
