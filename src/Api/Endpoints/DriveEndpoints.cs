using Lumora.Contracts.Drive;
using Lumora.Server.Api.Security;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Drive;
using Lumora.Server.Domain.Drive;
using Lumora.Server.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Lumora.Server.Api.Endpoints;

public static class DriveEndpoints
{
    /// <summary>Blobs stream through REST, never through SignalR — see plan §Api.</summary>
    private const long MaxBlobSizeBytes = 512L * 1024 * 1024;

    public static void MapDriveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rooms/{roomId:guid}/drive")
            .WithTags("Drive")
            .RequireAuthorization()
            .RequireRoomScope();

        group.MapGet("/", ListFiles);
        group.MapPost("/", RegisterFile);
        group.MapDelete("/{fileId:guid}", DeleteFile);

        group.MapPost("/blobs", UploadBlob).DisableAntiforgery();
        group.MapGet("/blobs/{blobId:guid}", DownloadBlob);
    }

    private static async Task<Ok<IReadOnlyList<DriveFileDto>>> ListFiles(Guid roomId, ISender sender, CancellationToken ct)
    {
        var files = await sender.Send(new ListDriveFilesQuery(roomId), ct);
        return TypedResults.Ok<IReadOnlyList<DriveFileDto>>(files.Select(ToDto).ToList());
    }

    private static async Task<Results<Ok<DriveFileDto>, ValidationProblem>> RegisterFile(
        Guid roomId, RegisterDriveFileRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterDriveFileCommand(roomId, request.BlobId, request.EncryptedMetadata, request.SizeBytes, request.DeviceId),
            ct);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] });
        }

        return TypedResults.Ok(ToDto(result.Value!));
    }

    private static async Task<Results<NoContent, ValidationProblem>> DeleteFile(
        Guid roomId, Guid fileId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteDriveFileCommand(roomId, fileId), ct);
        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<Guid>, BadRequest<string>>> UploadBlob(
        Guid roomId, HttpRequest request, IBlobStore blobStore, CancellationToken ct)
    {
        if (request.ContentLength is null || request.ContentLength > MaxBlobSizeBytes)
        {
            return TypedResults.BadRequest($"Blob przekracza maksymalny rozmiar {MaxBlobSizeBytes} B.");
        }

        var blobId = BlobId.New();
        await blobStore.SaveAsync(roomId, blobId, request.Body, ct);
        return TypedResults.Ok(blobId.Value);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> DownloadBlob(
        Guid roomId, Guid blobId, IBlobStore blobStore, CancellationToken ct)
    {
        try
        {
            var stream = await blobStore.OpenReadAsync(roomId, BlobId.From(blobId), ct);
            return TypedResults.Stream(stream, "application/octet-stream");
        }
        catch (FileNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static DriveFileDto ToDto(DriveFile file) => new(
        file.Id,
        file.EncryptedMetadata.Bytes,
        Guid.Parse(file.BlobId.ToString()),
        file.SizeBytes,
        file.DeviceId,
        file.CreatedAt);
}
