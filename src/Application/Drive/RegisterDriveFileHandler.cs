using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.Drive;
using Lumora.Server.Domain.ValueObjects;
using MediatR;

namespace Lumora.Server.Application.Drive;

public sealed class RegisterDriveFileHandler(
    IDriveRepository driveFiles,
    IRealtimeNotifier realtime,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<RegisterDriveFileCommand, Result<DriveFile>>
{
    public async Task<Result<DriveFile>> Handle(RegisterDriveFileCommand request, CancellationToken ct)
    {
        var metadataResult = EncryptedPayload.Create(request.EncryptedMetadata);
        if (!metadataResult.IsSuccess)
        {
            return Result<DriveFile>.Failure(metadataResult.Error!);
        }

        var fileResult = DriveFile.Create(
            request.RoomId, metadataResult.Value!, BlobId.From(request.BlobId), request.SizeBytes,
            request.DeviceId, clock.UtcNow);

        if (!fileResult.IsSuccess)
        {
            return fileResult;
        }

        var file = fileResult.Value!;
        driveFiles.Add(file);
        await unitOfWork.SaveChangesAsync(ct);
        await realtime.DriveFileAddedAsync(request.RoomId, file, ct);

        return Result<DriveFile>.Success(file);
    }
}
