using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Domain.Updates;

/// <summary>
/// One published Android build. There is no "current" flag — GetLatestAndroidReleaseQuery
/// always picks the row with the highest VersionCode, so publishing is append-only and a
/// bad release can be superseded by publishing a new one, never by mutating history.
/// </summary>
public sealed class UpdateRelease
{
    public Guid Id { get; }
    public string Version { get; }
    public int VersionCode { get; }
    public BlobId BlobId { get; }
    public long SizeBytes { get; }
    public string? ReleaseNotes { get; }
    public DateTimeOffset CreatedAt { get; }

    private UpdateRelease(
        Guid id, string version, int versionCode, BlobId blobId, long sizeBytes, string? releaseNotes, DateTimeOffset createdAt)
    {
        Id = id;
        Version = version;
        VersionCode = versionCode;
        BlobId = blobId;
        SizeBytes = sizeBytes;
        ReleaseNotes = releaseNotes;
        CreatedAt = createdAt;
    }

    public static Result<UpdateRelease> Create(
        string version, int versionCode, BlobId blobId, long sizeBytes, string? releaseNotes, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return Result<UpdateRelease>.Failure("Wersja nie może być pusta.");
        }

        if (versionCode <= 0)
        {
            return Result<UpdateRelease>.Failure("VersionCode musi być dodatni.");
        }

        if (sizeBytes <= 0)
        {
            return Result<UpdateRelease>.Failure("Rozmiar pliku APK musi być dodatni.");
        }

        return Result<UpdateRelease>.Success(
            new UpdateRelease(Guid.NewGuid(), version, versionCode, blobId, sizeBytes, releaseNotes, now));
    }
}
