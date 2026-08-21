using MediatR;

namespace Lumora.Server.Application.Updates;

public sealed record GetLatestAndroidReleaseQuery : IRequest<GetLatestAndroidReleaseResult?>;

public sealed record GetLatestAndroidReleaseResult(
    Guid ReleaseId, string Version, int VersionCode, Guid BlobId, long SizeBytes, string? ReleaseNotes, DateTimeOffset PublishedAt);
