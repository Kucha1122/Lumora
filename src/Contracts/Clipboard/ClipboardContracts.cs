namespace Lumora.Contracts.Clipboard;

public enum ClipboardEntryKindDto
{
    Text = 0,
    Image = 1
}

/// <summary>Payload is either inline ciphertext or, for large content, omitted in favor of BlobId.</summary>
public sealed record PushClipboardEntryRequest(
    ClipboardEntryKindDto Kind,
    Guid DeviceId,
    byte[]? InlinePayload,
    Guid? BlobId,
    int SizeBytes);

public sealed record ClipboardEntryDto(
    Guid Id,
    ClipboardEntryKindDto Kind,
    byte[]? InlinePayload,
    Guid? BlobId,
    int SizeBytes,
    Guid DeviceId,
    DateTimeOffset CreatedAt);
