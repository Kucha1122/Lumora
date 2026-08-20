using Lumora.Contracts.Clipboard;
using Lumora.Server.Api.Security;
using Lumora.Server.Application.Clipboard;
using Lumora.Server.Domain.Clipboard;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lumora.Server.Api.Endpoints;

public static class ClipboardEndpoints
{
    public static void MapClipboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms/{roomId:guid}/clipboard")
            .WithTags("Clipboard")
            .RequireAuthorization()
            .RequireRoomScope();

        group.MapGet("/", ListEntries);
        group.MapPost("/", PushEntry);
        group.MapDelete("/{entryId:guid}", DeleteEntry);
        group.MapDelete("/", ClearEntries);
    }

    private static async Task<Ok<IReadOnlyList<ClipboardEntryDto>>> ListEntries(
        Guid roomId, ISender sender, CancellationToken ct)
    {
        var entries = await sender.Send(new ListClipboardEntriesQuery(roomId), ct);
        return TypedResults.Ok<IReadOnlyList<ClipboardEntryDto>>(entries.Select(ToDto).ToList());
    }

    private static async Task<Results<Ok<ClipboardEntryDto>, ValidationProblem>> PushEntry(
        Guid roomId, PushClipboardEntryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new PushClipboardEntryCommand(
                roomId,
                (ClipboardEntryKind)request.Kind,
                request.InlinePayload,
                request.BlobId,
                request.SizeBytes,
                request.DeviceId),
            ct);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] });
        }

        return TypedResults.Ok(ToDto(result.Value!));
    }

    private static async Task<Results<NoContent, ValidationProblem>> DeleteEntry(
        Guid roomId, Guid entryId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteClipboardEntryCommand(roomId, entryId), ct);
        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] });
        }

        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ClearEntries(Guid roomId, ISender sender, CancellationToken ct)
    {
        await sender.Send(new ClearClipboardCommand(roomId), ct);
        return TypedResults.NoContent();
    }

    private static ClipboardEntryDto ToDto(ClipboardEntry entry) => new(
        entry.Id,
        (ClipboardEntryKindDto)entry.Kind,
        entry.InlinePayload?.Bytes,
        entry.BlobId is null ? null : Guid.Parse(entry.BlobId.ToString()),
        entry.SizeBytes,
        entry.DeviceId,
        entry.CreatedAt);
}
