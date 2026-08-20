using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Drive;
using Microsoft.EntityFrameworkCore;

namespace Lumora.Server.Infrastructure.Persistence.Repositories;

public sealed class DriveRepository(LumoraDbContext db) : IDriveRepository
{
    public async Task<IReadOnlyList<DriveFile>> ListAsync(Guid roomId, CancellationToken ct) =>
        await db.DriveFiles
            .Where(f => f.RoomId == roomId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

    public Task<DriveFile?> GetByIdAsync(Guid roomId, Guid fileId, CancellationToken ct) =>
        db.DriveFiles.FirstOrDefaultAsync(f => f.RoomId == roomId && f.Id == fileId, ct);

    public void Add(DriveFile file) => db.DriveFiles.Add(file);

    public void Remove(DriveFile file) => db.DriveFiles.Remove(file);
}
