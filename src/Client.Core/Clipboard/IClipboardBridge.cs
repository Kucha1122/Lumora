namespace Lumora.Client.Core.Clipboard;

public enum LocalClipboardContentKind
{
    Text,
    Image
}

public sealed record LocalClipboardContent(LocalClipboardContentKind Kind, byte[] Data);

/// <summary>Platform-specific access to the OS clipboard. One implementation per client shell.</summary>
public interface IClipboardBridge
{
    event Func<LocalClipboardContent, Task>? ContentChanged;

    Task SetContentAsync(LocalClipboardContent content, CancellationToken ct);

    void Start();

    void Stop();
}
