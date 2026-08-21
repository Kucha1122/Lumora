using Lumora.Client.Core.Rooms;

namespace Lumora.Client.Android;

public partial class App : Application
{
    private readonly RoomSessionService roomSession;
    private bool initialized;

    public App(RoomSessionService roomSession)
    {
        InitializeComponent();
        this.roomSession = roomSession;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // No foreground service (see plan §Dlaczego bez foreground service): the app fetches a
        // fresh history and reconnects realtime only while it actually has focus, and drops the
        // connection the moment it doesn't. Resumed/Stopped are MAUI's cross-platform stand-ins
        // for Activity.OnResume/OnStop.
        window.Resumed += async (_, _) => await OnForegroundedAsync();
        window.Stopped += (_, _) => OnBackgrounded();

        return window;
    }

    private async Task OnForegroundedAsync()
    {
        try
        {
            if (!initialized)
            {
                initialized = true;
                await roomSession.InitializeAsync(CancellationToken.None);
            }
            else
            {
                await roomSession.ReconnectActiveRoomAsync(CancellationToken.None);
            }
        }
        catch
        {
            // Server unreachable (no network, Tailscale down) — pages surface their own
            // "brak połączenia" state via reload failures; the app must still open.
        }
    }

    private void OnBackgrounded() => roomSession.DisconnectRealtime();
}
