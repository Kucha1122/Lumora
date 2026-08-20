using System.Runtime.InteropServices;

namespace Lumora.Client.Desktop.Clipboard;

/// <summary>Win32 P/Invoke surface for a message-only clipboard listener window.
/// Avalonia has no clipboard-change API, so this is unavoidable on Windows.</summary>
internal static partial class NativeMethods
{
    public const int WM_CLIPBOARDUPDATE = 0x031D;
    public const int WM_DESTROY = 0x0002;
    public const int WM_HOTKEY = 0x0312;
    public const int HWND_MESSAGE = -3;

    public const uint CF_UNICODETEXT = 13;
    public const uint CF_DIB = 8;

    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_NOREPEAT = 0x4000;
    public const uint VK_OEM_5 = 0xDC; // The key labeled "|\" on a US keyboard, next to Backspace.
    public const ushort VK_CONTROL = 0x11;
    public const ushort VK_V = 0x56;

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEX
    {
        public int cbSize;
        public int style;
        public WndProcDelegate lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public nint hIconSm;
    }

    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AddClipboardFormatListener(nint hWnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RemoveClipboardFormatListener(nint hWnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    public static partial uint GetClipboardSequenceNumber();

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsClipboardFormatAvailable(uint format);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalUnlock(nint hMem);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nuint GlobalSize(nint hMem);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [LibraryImport("user32.dll")]
    public static partial nint DispatchMessageW(ref MSG lpMsg);

    [LibraryImport("user32.dll")]
    public static partial void PostQuitMessage(int nExitCode);

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    public const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(nint hWnd, int id);

    [LibraryImport("user32.dll")]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
        // KEYBDINPUT is smaller than the union's largest member (MOUSEINPUT/HARDWAREINPUT);
        // this padding keeps the struct's total size correct for SendInput on 64-bit.
        private long padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);
}
