using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Lumora.Client.Core.Clipboard;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Sync;
using Lumora.Client.Core.Transport;
using Lumora.Client.Desktop.Clipboard;
using Lumora.Client.Desktop.Rooms;
using Lumora.Client.Desktop.Windows;

namespace Lumora.Client.Desktop;

public partial class App : Application
{
    private LumoraApiClient api = null!;
    private LumoraRealtimeClient realtime = null!;
    private ActiveRoomStore activeRoomStore = null!;
    private RoomSessionService roomSession = null!;
    private WindowsClipboardBridge clipboardBridge = null!;
    private ClipboardSyncEngine syncEngine = null!;
    private TrayIcon trayIcon = null!;
    private NativeMenuItem currentRoomMenuItem = null!;
    private Guid deviceId;

    private readonly WindowSlot<ClipboardHistoryWindow> historySlot = new();
    private readonly WindowSlot<DriveWindow> driveSlot = new();
    private readonly WindowSlot<RoomWindow> roomSlot = new();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Tray-first: no main window ever appears at startup, and closing every
            // secondary window must not quit the app — only the tray "Wyjdź" does.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = ComposeAndStartAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task ComposeAndStartAsync()
    {
        var serverBaseAddress = LoadServerBaseAddress();
        deviceId = DeviceIdentity.GetOrCreate();

        var httpClient = new HttpClient { BaseAddress = serverBaseAddress };
        api = new LumoraApiClient(httpClient);
        realtime = new LumoraRealtimeClient();

        ISecureStorage secureStorage = OperatingSystem.IsWindows()
            ? new DpapiSecureStorage()
            : throw new PlatformNotSupportedException("Ten klient obsługuje na razie tylko Windows.");

        activeRoomStore = new ActiveRoomStore(secureStorage);

        clipboardBridge = new WindowsClipboardBridge();
        clipboardBridge.QuickPasteRequested += foregroundWindow =>
            Dispatcher.UIThread.Post(() => ShowQuickPastePopup(foregroundWindow));
        clipboardBridge.Start();

        syncEngine = new ClipboardSyncEngine(clipboardBridge, api, realtime, activeRoomStore);

        var hubUri = new Uri(serverBaseAddress, "/hubs/room");
        roomSession = new RoomSessionService(api, realtime, activeRoomStore, syncEngine, hubUri);

        BuildTrayIcon();
        activeRoomStore.ActiveRoomChanged += _ => UpdateTrayAppearance();

        await roomSession.InitializeAsync(CancellationToken.None);
        UpdateTrayAppearance();
    }

    private static Uri LoadServerBaseAddress()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
        {
            return new Uri("http://localhost:5108");
        }

        using var stream = File.OpenRead(settingsPath);
        using var document = JsonDocument.Parse(stream);
        var address = document.RootElement.GetProperty("ServerBaseAddress").GetString();
        return new Uri(address ?? "http://localhost:5108");
    }

    private void BuildTrayIcon()
    {
        trayIcon = new TrayIcon { Icon = TrayIconFactory.CreatePublicIcon(), ToolTipText = "Lumora" };

        // Always visible the moment you right-click the tray icon — the one place the user
        // is guaranteed to see which room is active without opening a separate window.
        currentRoomMenuItem = new NativeMenuItem { IsEnabled = false };

        var historyItem = new NativeMenuItem("Historia schowka");
        historyItem.Click += (_, _) => ShowSingleton(
            historySlot, () => new ClipboardHistoryWindow(syncEngine.History, syncEngine, activeRoomStore));

        var driveItem = new NativeMenuItem("Drive");
        driveItem.Click += (_, _) => ShowSingleton(
            driveSlot, () => new DriveWindow(api, activeRoomStore, deviceId));

        var switchItem = new NativeMenuItem("Zmień przestrzeń…");
        switchItem.Click += (_, _) => ShowSingleton(roomSlot, () => new RoomWindow(roomSession, api, activeRoomStore));

        var exitItem = new NativeMenuItem("Wyjdź");
        exitItem.Click += (_, _) =>
        {
            clipboardBridge.Stop();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };

        trayIcon.Menu = new NativeMenu
        {
            currentRoomMenuItem, new NativeMenuItemSeparator(),
            historyItem, driveItem, switchItem, new NativeMenuItemSeparator(),
            exitItem
        };

        var icons = new TrayIcons { trayIcon };
        TrayIcon.SetIcons(this, icons);
    }

    private void UpdateTrayAppearance()
    {
        var room = activeRoomStore.ActiveRoom;
        trayIcon.Icon = room is { IsPrivate: true } ? TrayIconFactory.CreatePrivateIcon() : TrayIconFactory.CreatePublicIcon();
        trayIcon.ToolTipText = room is null ? "Lumora" : $"Lumora — {room.DisplayName}{(room.IsPrivate ? " (prywatna)" : "")}";
        currentRoomMenuItem.Header = room is null
            ? "Brak aktywnej przestrzeni"
            : $"{(room.IsPrivate ? "🔒" : "🌐")} {room.DisplayName}";
    }

    private void ShowQuickPastePopup(nint foregroundWindow)
    {
        var recent = syncEngine.History.Items.Take(10).ToList();
        if (recent.Count == 0)
        {
            return;
        }

        var popup = new QuickPastePopup(recent, async item =>
        {
            syncEngine.SuppressNextLocalChange(item.Plaintext);
            await clipboardBridge.PasteIntoForegroundWindowAsync(
                new LocalClipboardContent(item.Kind, item.Plaintext), foregroundWindow);
        });

        var (cursorX, cursorY) = WindowsClipboardBridge.GetCursorPosition();
        popup.ShowAt(cursorX, cursorY);
    }

    /// <summary>Holds at most one live instance of a given window type.</summary>
    private sealed class WindowSlot<TWindow> where TWindow : Window
    {
        public TWindow? Value;
    }

    /// <summary>Reuses an already-open window instead of stacking duplicates — clicking
    /// "Historia schowka" a second time should bring the existing one forward, not open
    /// another. The slot is cleared on Closed so a fresh window is created next time.</summary>
    private static void ShowSingleton<TWindow>(WindowSlot<TWindow> slot, Func<TWindow> factory) where TWindow : Window
    {
        if (slot.Value is not null)
        {
            slot.Value.Activate();
            if (slot.Value.WindowState == WindowState.Minimized)
            {
                slot.Value.WindowState = WindowState.Normal;
            }

            return;
        }

        var window = factory();
        window.Closed += (_, _) => slot.Value = null;
        slot.Value = window;
        window.Show();
        window.Activate();
    }
}
