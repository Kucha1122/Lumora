using Lumora.Client.Core.Clipboard;

namespace Lumora.Client.Core.Sync;

/// <summary>A decrypted, ready-to-paste clipboard history entry — never persisted, kept only in memory.</summary>
public sealed record ClipboardHistoryItem(
    Guid EntryId,
    LocalClipboardContentKind Kind,
    byte[] Plaintext,
    Guid DeviceId,
    DateTimeOffset CreatedAt);
