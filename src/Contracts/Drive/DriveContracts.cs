namespace Lumora.Contracts.Drive;

/// <summary>Client uploads the blob first via streaming REST, then registers metadata with this.</summary>
public sealed record RegisterDriveFileRequest(
    Guid BlobId,
    byte[] EncryptedMetadata,
    long SizeBytes,
    Guid DeviceId);

public sealed record DriveFileDto(
    Guid Id,
    byte[] EncryptedMetadata,
    Guid BlobId,
    long SizeBytes,
    Guid DeviceId,
    DateTimeOffset CreatedAt);
