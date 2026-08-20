using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Domain.Drive;

/// <summary>
/// A file registered in a room's drive. The filename and MIME type travel as
/// <see cref="EncryptedMetadata"/> — the server never sees plaintext names.
/// </summary>
public sealed class DriveFile
{
    public Guid Id { get; }
    public Guid RoomId { get; }
    public EncryptedPayload EncryptedMetadata { get; }
    public BlobId BlobId { get; }
    public long SizeBytes { get; }
    public Guid DeviceId { get; }
    public DateTimeOffset CreatedAt { get; }

    private DriveFile(
        Guid id,
        Guid roomId,
        EncryptedPayload encryptedMetadata,
        BlobId blobId,
        long sizeBytes,
        Guid deviceId,
        DateTimeOffset createdAt)
    {
        Id = id;
        RoomId = roomId;
        EncryptedMetadata = encryptedMetadata;
        BlobId = blobId;
        SizeBytes = sizeBytes;
        DeviceId = deviceId;
        CreatedAt = createdAt;
    }

    public static Result<DriveFile> Create(
        Guid roomId, EncryptedPayload encryptedMetadata, BlobId blobId, long sizeBytes, Guid deviceId,
        DateTimeOffset now)
    {
        if (sizeBytes <= 0)
        {
            return Result<DriveFile>.Failure("Rozmiar pliku musi być większy od zera.");
        }

        return Result<DriveFile>.Success(
            new DriveFile(Guid.NewGuid(), roomId, encryptedMetadata, blobId, sizeBytes, deviceId, now));
    }
}
