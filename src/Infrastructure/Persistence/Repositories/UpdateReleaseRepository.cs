using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Updates;
using Microsoft.EntityFrameworkCore;

namespace Lumora.Server.Infrastructure.Persistence.Repositories;

public sealed class UpdateReleaseRepository(LumoraDbContext db) : IUpdateReleaseRepository
{
    public Task<UpdateRelease?> GetLatestAsync(CancellationToken ct) =>
        db.UpdateReleases.OrderByDescending(r => r.VersionCode).FirstOrDefaultAsync(ct);

    public void Add(UpdateRelease release) => db.UpdateReleases.Add(release);
}
