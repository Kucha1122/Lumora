using Lumora.Server.Domain.Updates;

namespace Lumora.Server.Application.Abstractions.Persistence;

public interface IUpdateReleaseRepository
{
    /// <summary>Highest VersionCode, or null if nothing has been published yet.</summary>
    Task<UpdateRelease?> GetLatestAsync(CancellationToken ct);

    void Add(UpdateRelease release);
}
