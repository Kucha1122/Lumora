using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Clipboard;

public sealed record ClearClipboardCommand(Guid RoomId) : IRequest<Result>;

public sealed class ClearClipboardHandler(
    IClipboardRepository clipboard,
    IBlobStore blobStore,
    IRealtimeNotifier realtime,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearClipboardCommand, Result>
{
    public async Task<Result> Handle(ClearClipboardCommand request, CancellationToken ct)
    {
        var entries = await clipboard.ListAllAsync(request.RoomId, ct);

        foreach (var entry in entries)
        {
            clipboard.Remove(entry);
            if (entry.BlobId is not null)
            {
                await blobStore.DeleteAsync(request.RoomId, entry.BlobId, ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        await realtime.ClipboardClearedAsync(request.RoomId, ct);

        return Result.Success();
    }
}
