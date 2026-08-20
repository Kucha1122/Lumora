using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Clipboard;

public sealed record DeleteClipboardEntryCommand(Guid RoomId, Guid EntryId) : IRequest<Result>;

public sealed class DeleteClipboardEntryHandler(
    IClipboardRepository clipboard,
    IBlobStore blobStore,
    IRealtimeNotifier realtime,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteClipboardEntryCommand, Result>
{
    public async Task<Result> Handle(DeleteClipboardEntryCommand request, CancellationToken ct)
    {
        var entry = await clipboard.GetByIdAsync(request.RoomId, request.EntryId, ct);
        if (entry is null)
        {
            return Result.Failure("Nie znaleziono wpisu.");
        }

        clipboard.Remove(entry);
        if (entry.BlobId is not null)
        {
            await blobStore.DeleteAsync(request.RoomId, entry.BlobId, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        await realtime.ClipboardEntryDeletedAsync(request.RoomId, entry.Id, ct);

        return Result.Success();
    }
}
