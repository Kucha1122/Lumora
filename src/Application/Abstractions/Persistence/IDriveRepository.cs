using Lumora.Server.Domain.Drive;

namespace Lumora.Server.Application.Abstractions.Persistence;

public interface IDriveRepository
{
    Task<IReadOnlyList<DriveFile>> ListAsync(Guid roomId, CancellationToken ct);

    Task<DriveFile?> GetByIdAsync(Guid roomId, Guid fileId, CancellationToken ct);

    void Add(DriveFile file);

    void Remove(DriveFile file);
}
