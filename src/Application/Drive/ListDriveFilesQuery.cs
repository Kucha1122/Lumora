using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Drive;
using MediatR;

namespace Lumora.Server.Application.Drive;

public sealed record ListDriveFilesQuery(Guid RoomId) : IRequest<IReadOnlyList<DriveFile>>;

public sealed class ListDriveFilesHandler(IDriveRepository driveFiles)
    : IRequestHandler<ListDriveFilesQuery, IReadOnlyList<DriveFile>>
{
    public Task<IReadOnlyList<DriveFile>> Handle(ListDriveFilesQuery request, CancellationToken ct) =>
        driveFiles.ListAsync(request.RoomId, ct);
}
