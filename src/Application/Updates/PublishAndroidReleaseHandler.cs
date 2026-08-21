using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.Updates;
using Lumora.Server.Domain.ValueObjects;
using MediatR;

namespace Lumora.Server.Application.Updates;

public sealed class PublishAndroidReleaseHandler(
    IUpdateReleaseRepository releases,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<PublishAndroidReleaseCommand, Result<PublishAndroidReleaseResult>>
{
    public async Task<Result<PublishAndroidReleaseResult>> Handle(PublishAndroidReleaseCommand request, CancellationToken ct)
    {
        var latest = await releases.GetLatestAsync(ct);
        if (latest is not null && request.VersionCode <= latest.VersionCode)
        {
            return Result<PublishAndroidReleaseResult>.Failure(
                $"VersionCode {request.VersionCode} nie jest wyższy niż już opublikowany {latest.VersionCode}.");
        }

        var releaseResult = UpdateRelease.Create(
            request.Version, request.VersionCode, BlobId.From(request.BlobId), request.SizeBytes, request.ReleaseNotes, clock.UtcNow);

        if (!releaseResult.IsSuccess)
        {
            return Result<PublishAndroidReleaseResult>.Failure(releaseResult.Error!);
        }

        var release = releaseResult.Value!;
        releases.Add(release);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<PublishAndroidReleaseResult>.Success(
            new PublishAndroidReleaseResult(release.Id, release.Version, release.VersionCode));
    }
}
