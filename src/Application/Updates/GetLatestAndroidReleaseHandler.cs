using Lumora.Server.Application.Abstractions.Persistence;
using MediatR;

namespace Lumora.Server.Application.Updates;

public sealed class GetLatestAndroidReleaseHandler(IUpdateReleaseRepository releases)
    : IRequestHandler<GetLatestAndroidReleaseQuery, GetLatestAndroidReleaseResult?>
{
    public async Task<GetLatestAndroidReleaseResult?> Handle(GetLatestAndroidReleaseQuery request, CancellationToken ct)
    {
        var release = await releases.GetLatestAsync(ct);
        return release is null
            ? null
            : new GetLatestAndroidReleaseResult(
                release.Id, release.Version, release.VersionCode, release.BlobId.Value, release.SizeBytes, release.ReleaseNotes, release.CreatedAt);
    }
}
