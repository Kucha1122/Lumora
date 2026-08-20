using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

namespace Lumora.Server.Application.Clipboard;

public sealed class PushClipboardEntryHandler(
    IClipboardRepository clipboard,
    IBlobStore blobStore,
    IRealtimeNotifier realtime,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IOptions<ClipboardRetentionOptions> retentionOptions) : IRequestHandler<PushClipboardEntryCommand, Result<ClipboardEntry>>
{
    private readonly ClipboardRetentionOptions retention = retentionOptions.Value;

    public async Task<Result<ClipboardEntry>> Handle(PushClipboardEntryCommand request, CancellationToken ct)
    {
        Result<ClipboardEntry> entryResult;

        if (request.BlobId is { } blobIdGuid)
        {
            entryResult = ClipboardEntry.CreateFromBlob(
                request.RoomId, request.Kind, BlobId.From(blobIdGuid), request.SizeBytes, request.DeviceId,
                clock.UtcNow);
        }
        else if (request.InlinePayload is not null)
        {
            var payloadResult = EncryptedPayload.Create(request.InlinePayload);
            if (!payloadResult.IsSuccess)
            {
                return Result<ClipboardEntry>.Failure(payloadResult.Error!);
            }

            entryResult = ClipboardEntry.CreateInline(
                request.RoomId, request.Kind, payloadResult.Value!, request.DeviceId, clock.UtcNow);
        }
        else
        {
            return Result<ClipboardEntry>.Failure("Wpis musi mieć payload inline albo referencję do bloba.");
        }

        if (!entryResult.IsSuccess)
        {
            return entryResult;
        }

        var entry = entryResult.Value!;
        clipboard.Add(entry);

        await EnforceRetentionAsync(request.RoomId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await realtime.ClipboardEntryPushedAsync(request.RoomId, entry, ct);

        return Result<ClipboardEntry>.Success(entry);
    }

    private async Task EnforceRetentionAsync(Guid roomId, CancellationToken ct)
    {
        var overflow = await clipboard.ListOverflowAsync(
            roomId, retention.MaxEntriesPerRoom, retention.MaxAge, clock.UtcNow, ct);

        foreach (var stale in overflow)
        {
            clipboard.Remove(stale);
            if (stale.BlobId is not null)
            {
                await blobStore.DeleteAsync(roomId, stale.BlobId, ct);
            }
        }
    }
}
