using Lumora.Server.Domain.Common;
using MediatR;

namespace Lumora.Server.Application.Updates;

public sealed record PublishAndroidReleaseCommand(
    string Version,
    int VersionCode,
    Guid BlobId,
    long SizeBytes,
    string? ReleaseNotes) : IRequest<Result<PublishAndroidReleaseResult>>;

public sealed record PublishAndroidReleaseResult(Guid ReleaseId, string Version, int VersionCode);
