using Lumora.Server.Application.Abstractions.Persistence;

namespace Lumora.Server.Infrastructure.Persistence;

public sealed class UnitOfWork(LumoraDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
