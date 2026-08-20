using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Domain.Clipboard;

/// <summary>
/// A single push to a room's shared clipboard. Small payloads are stored inline;
/// large ones (e.g. images) are stored as a blob referenced by <see cref="BlobId"/>.
/// </summary>
public sealed class ClipboardEntry
{
    /// <summary>Payloads at or under this size are stored inline instead of as a blob.</summary>
    public const int InlineThresholdBytes = 256 * 1024;

    public Guid Id { get; }
    public Guid RoomId { get; }
    public ClipboardEntryKind Kind { get; }
    public EncryptedPayload? InlinePayload { get; }
    public BlobId? BlobId { get; }
    public int SizeBytes { get; }
    public Guid DeviceId { get; }
    public DateTimeOffset CreatedAt { get; }

    private ClipboardEntry(
        Guid id,
        Guid roomId,
        ClipboardEntryKind kind,
        EncryptedPayload? inlinePayload,
        BlobId? blobId,
        int sizeBytes,
        Guid deviceId,
        DateTimeOffset createdAt)
    {
        Id = id;
        RoomId = roomId;
        Kind = kind;
        InlinePayload = inlinePayload;
        BlobId = blobId;
        SizeBytes = sizeBytes;
        DeviceId = deviceId;
        CreatedAt = createdAt;
    }

    public static Result<ClipboardEntry> CreateInline(
        Guid roomId, ClipboardEntryKind kind, EncryptedPayload payload, Guid deviceId, DateTimeOffset now)
    {
        if (payload.SizeBytes > InlineThresholdBytes)
        {
            return Result<ClipboardEntry>.Failure(
                $"Payload przekracza próg inline ({InlineThresholdBytes} B) — użyj CreateFromBlob.");
        }

        return Result<ClipboardEntry>.Success(new ClipboardEntry(
            Guid.NewGuid(), roomId, kind, payload, null, payload.SizeBytes, deviceId, now));
    }

    public static Result<ClipboardEntry> CreateFromBlob(
        Guid roomId, ClipboardEntryKind kind, BlobId blobId, int sizeBytes, Guid deviceId, DateTimeOffset now)
    {
        if (sizeBytes <= 0)
        {
            return Result<ClipboardEntry>.Failure("Rozmiar bloba musi być większy od zera.");
        }

        return Result<ClipboardEntry>.Success(new ClipboardEntry(
            Guid.NewGuid(), roomId, kind, null, blobId, sizeBytes, deviceId, now));
    }
}
