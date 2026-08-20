namespace Lumora.Server.Application.Clipboard;

public sealed class ClipboardRetentionOptions
{
    public int MaxEntriesPerRoom { get; init; } = 100;

    public TimeSpan MaxAge { get; init; } = TimeSpan.FromDays(7);
}
