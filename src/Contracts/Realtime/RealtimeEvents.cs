using Lumora.Contracts.Clipboard;
using Lumora.Contracts.Drive;

namespace Lumora.Contracts.Realtime;

public static class RealtimeHubMethods
{
    public const string ClipboardEntryPushed = nameof(ClipboardEntryPushed);
    public const string ClipboardEntryDeleted = nameof(ClipboardEntryDeleted);
    public const string ClipboardCleared = nameof(ClipboardCleared);
    public const string DriveFileAdded = nameof(DriveFileAdded);
    public const string DriveFileDeleted = nameof(DriveFileDeleted);
}

public sealed record ClipboardEntryPushedEvent(ClipboardEntryDto Entry);

public sealed record ClipboardEntryDeletedEvent(Guid EntryId);

public sealed record ClipboardClearedEvent;

public sealed record DriveFileAddedEvent(DriveFileDto File);

public sealed record DriveFileDeletedEvent(Guid FileId);
