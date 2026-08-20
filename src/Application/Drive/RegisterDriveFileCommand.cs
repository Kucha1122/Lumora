using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.Drive;
using MediatR;

namespace Lumora.Server.Application.Drive;

/// <summary>
/// The blob itself is uploaded separately via streaming REST before this command runs —
/// this only registers its (encrypted) metadata once the upload has succeeded.
/// </summary>
public sealed record RegisterDriveFileCommand(
    Guid RoomId,
    Guid BlobId,
    byte[] EncryptedMetadata,
    long SizeBytes,
    Guid DeviceId) : IRequest<Result<DriveFile>>;
