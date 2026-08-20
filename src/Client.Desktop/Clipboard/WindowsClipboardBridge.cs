using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Lumora.Client.Core.Clipboard;
using static Lumora.Client.Desktop.Clipboard.NativeMethods;

namespace Lumora.Client.Desktop.Clipboard;

/// <summary>
/// Avalonia has no clipboard-change notification, so this listens directly via Win32:
/// a message-only window (HWND_MESSAGE, invisible, no taskbar entry) registered with
/// AddClipboardFormatListener receives WM_CLIPBOARDUPDATE whenever any app changes the
/// clipboard. The window and its message loop live on a dedicated STA thread because
/// window handles are thread-affine and this must not block the Avalonia UI thread.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsClipboardBridge : IClipboardBridge, IDisposable
{
    private const string WindowClassName = "LumoraClipboardListener";
    private const int QuickPasteHotkeyId = 1;

    private Thread? messageThread;
    private nint windowHandle;
    private WndProcDelegate? wndProcDelegate;
    private volatile bool isRunning;

    /// <summary>
    /// The clipboard's sequence number right after this bridge's own last write — Windows
    /// bumps this counter on every clipboard change and hands the current value to anyone
    /// who asks, so comparing against it (rather than a one-shot flag) survives a single
    /// write raising more than one WM_CLIPBOARDUPDATE (EmptyClipboard + SetClipboardData
    /// each count as a change) without a race between which notification "consumes" the flag.
    /// A byte-hash comparison (the client-side LoopGuard) isn't reliable enough for this on
    /// its own: an image written as PNG comes back through Windows as CF_DIB and gets
    /// re-encoded to PNG on read, and that re-encoding essentially never reproduces the
    /// original PNG bytes even though the pixels are identical.
    ///
    /// Reading/writing this field is guarded by <see cref="clipboardWriteLock"/>, not just
    /// `volatile` — a plain flag/field isn't enough on its own. Writing an image is slow
    /// enough (large DIB payload) that the STA listener thread can process the resulting
    /// WM_CLIPBOARDUPDATE and read this field *before* the writing thread has finished
    /// assigning it, silently defeating the whole check. Text is small enough that this race
    /// essentially never loses, which is why only images were visibly duplicating. Taking
    /// the lock for the whole write forces the listener to wait until the write (and the
    /// assignment) is fully done before it's allowed to compare against it.
    /// </summary>
    private uint lastSelfWriteSequence = uint.MaxValue;

    private readonly object clipboardWriteLock = new();

    public event Func<LocalClipboardContent, Task>? ContentChanged;

    /// <summary>Fired on the clipboard-listener thread when Ctrl+\ is pressed anywhere in
    /// Windows. The argument is whichever window had focus at that instant — captured
    /// immediately, before a popup can steal it, so paste-back knows where to go.</summary>
    public event Action<nint>? QuickPasteRequested;

    public void Start()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        messageThread = new Thread(RunMessageLoop) { IsBackground = true, Name = "Lumora.ClipboardListener" };
        messageThread.SetApartmentState(ApartmentState.STA);
        messageThread.Start();
    }

    public void Stop()
    {
        if (!isRunning || windowHandle == 0)
        {
            return;
        }

        isRunning = false;
        UnregisterHotKey(windowHandle, QuickPasteHotkeyId);
        RemoveClipboardFormatListener(windowHandle);
        DestroyWindow(windowHandle);
        windowHandle = 0;
    }

    /// <summary>Writes to the OS clipboard and simulates Ctrl+V into whichever window last had
    /// focus — used by the quick-paste popup, which itself steals focus while it's open.</summary>
    public async Task PasteIntoForegroundWindowAsync(LocalClipboardContent content, nint targetWindow)
    {
        await SetContentAsync(content, CancellationToken.None);

        SetForegroundWindow(targetWindow);
        await Task.Delay(50); // Give the target window a moment to actually regain focus.

        var inputs = new[]
        {
            KeyInput(VK_CONTROL, keyUp: false),
            KeyInput(VK_V, keyUp: false),
            KeyInput(VK_V, keyUp: true),
            KeyInput(VK_CONTROL, keyUp: true)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public static (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out var point);
        return (point.X, point.Y);
    }

    private static INPUT KeyInput(ushort vk, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        ki = new KEYBDINPUT { wVk = vk, dwFlags = keyUp ? KEYEVENTF_KEYUP : 0 }
    };

    public Task SetContentAsync(LocalClipboardContent content, CancellationToken ct)
    {
        // Do the (potentially slow, for a large image) PNG<->DIB conversion before taking
        // the lock, so the critical section — the part HandleClipboardUpdate has to wait
        // out — stays as short as possible.
        var dib = content.Kind == LocalClipboardContentKind.Image ? ClipboardImageCodec.PngToDib(content.Data) : null;

        lock (clipboardWriteLock)
        {
            if (!OpenClipboard(windowHandle))
            {
                throw new InvalidOperationException("Nie udało się otworzyć schowka.");
            }

            try
            {
                EmptyClipboard();

                if (content.Kind == LocalClipboardContentKind.Text)
                {
                    SetUnicodeText(Encoding.UTF8.GetString(content.Data));
                }
                else
                {
                    SetDib(dib!);
                }
            }
            finally
            {
                CloseClipboard();
            }

            // Windows finalizes the sequence-number bump on CloseClipboard, not on the
            // EmptyClipboard/SetClipboardData calls that precede it — reading the number
            // before closing captured the pre-increment (stale) value every single time,
            // which is why this comparison failed 100% of the time, not intermittently.
            lastSelfWriteSequence = GetClipboardSequenceNumber();
        }

        return Task.CompletedTask;
    }

    private void RunMessageLoop()
    {
        wndProcDelegate = WndProc;

        var wndClass = new WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = wndProcDelegate,
            lpszClassName = WindowClassName
        };

        RegisterClassEx(ref wndClass);

        windowHandle = CreateWindowEx(
            0, WindowClassName, "Lumora Clipboard Listener", 0,
            0, 0, 0, 0, HWND_MESSAGE, 0, 0, 0);

        if (windowHandle == 0)
        {
            isRunning = false;
            return;
        }

        AddClipboardFormatListener(windowHandle);
        RegisterHotKey(windowHandle, QuickPasteHotkeyId, MOD_CONTROL | MOD_NOREPEAT, VK_OEM_5);

        while (GetMessageW(out var msg, 0, 0, 0) > 0)
        {
            DispatchMessageW(ref msg);
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            HandleClipboardUpdate();
            return 0;
        }

        if (msg == WM_HOTKEY && (int)wParam == QuickPasteHotkeyId)
        {
            QuickPasteRequested?.Invoke(GetForegroundWindow());
            return 0;
        }

        if (msg == WM_DESTROY)
        {
            PostQuitMessage(0);
            return 0;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void HandleClipboardUpdate()
    {
        // Blocks until any in-flight SetContentAsync (on another thread) has fully finished
        // writing and recorded its resulting sequence number — see lastSelfWriteSequence's
        // doc comment for why this can't be a lock-free check.
        lock (clipboardWriteLock)
        {
            if (GetClipboardSequenceNumber() == lastSelfWriteSequence)
            {
                return;
            }
        }

        LocalClipboardContent? content = TryReadClipboard();
        if (content is not null)
        {
            _ = ContentChanged?.Invoke(content);
        }
    }

    private LocalClipboardContent? TryReadClipboard()
    {
        if (!OpenClipboard(windowHandle))
        {
            return null;
        }

        try
        {
            if (IsClipboardFormatAvailable(CF_UNICODETEXT))
            {
                var text = ReadUnicodeText();
                return text is null ? null : new LocalClipboardContent(LocalClipboardContentKind.Text, Encoding.UTF8.GetBytes(text));
            }

            if (IsClipboardFormatAvailable(CF_DIB))
            {
                var dib = ReadDib();
                if (dib is null)
                {
                    return null;
                }

                var png = ClipboardImageCodec.DibToPng(dib);
                return new LocalClipboardContent(LocalClipboardContentKind.Image, png);
            }

            return null;
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string? ReadUnicodeText()
    {
        var handle = GetClipboardData(CF_UNICODETEXT);
        if (handle == 0)
        {
            return null;
        }

        var pointer = GlobalLock(handle);
        if (pointer == 0)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUni(pointer);
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static byte[]? ReadDib()
    {
        var handle = GetClipboardData(CF_DIB);
        if (handle == 0)
        {
            return null;
        }

        var pointer = GlobalLock(handle);
        if (pointer == 0)
        {
            return null;
        }

        try
        {
            var size = (int)GlobalSize(handle);
            var bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, size);
            return bytes;
        }
        finally
        {
            GlobalUnlock(handle);
        }
    }

    private static void SetUnicodeText(string text)
    {
        var bytes = (text.Length + 1) * 2;
        var handle = GlobalAlloc(GMEM_MOVEABLE, (nuint)bytes);
        var pointer = GlobalLock(handle);
        try
        {
            var chars = text.ToCharArray();
            Marshal.Copy(chars, 0, pointer, chars.Length);
            Marshal.WriteInt16(pointer, chars.Length * 2, 0);
        }
        finally
        {
            GlobalUnlock(handle);
        }

        SetClipboardData(CF_UNICODETEXT, handle);
    }

    private static void SetDib(byte[] dib)
    {
        var handle = GlobalAlloc(GMEM_MOVEABLE, (nuint)dib.Length);
        var pointer = GlobalLock(handle);
        try
        {
            Marshal.Copy(dib, 0, pointer, dib.Length);
        }
        finally
        {
            GlobalUnlock(handle);
        }

        SetClipboardData(CF_DIB, handle);
    }

    public void Dispose() => Stop();
}
