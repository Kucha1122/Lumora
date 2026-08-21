namespace Lumora.Contracts.Updates;

public sealed record AndroidReleaseDto(
    string Version, int VersionCode, long SizeBytes, string? ReleaseNotes, DateTimeOffset PublishedAt);

/// <summary>Second step of publishing, after the raw APK bytes were uploaded via
/// POST /updates/android/blobs and its returned BlobId is passed in here — same
/// two-step shape as Drive's upload-then-register flow.</summary>
public sealed record PublishAndroidReleaseRequest(
    string Version, int VersionCode, Guid BlobId, long SizeBytes, string? ReleaseNotes);
