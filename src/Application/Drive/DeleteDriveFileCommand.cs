using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Drive;

public sealed record DeleteDriveFileCommand(Guid RoomId, Guid FileId) : IRequest<Result>;

public sealed class DeleteDriveFileHandler(
    IDriveRepository driveFiles,
    IBlobStore blobStore,
    IRealtimeNotifier realtime,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteDriveFileCommand, Result>
{
    public async Task<Result> Handle(DeleteDriveFileCommand request, CancellationToken ct)
    {
        var file = await driveFiles.GetByIdAsync(request.RoomId, request.FileId, ct);
        if (file is null)
        {
            return Result.Failure("Nie znaleziono pliku.");
        }

        driveFiles.Remove(file);
        await blobStore.DeleteAsync(request.RoomId, file.BlobId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await realtime.DriveFileDeletedAsync(request.RoomId, file.Id, ct);

        return Result.Success();
    }
}
