using Lumora.Client.Android.Clipboard;
using Lumora.Client.Android.Pages;
using Lumora.Client.Android.Rooms;
using Lumora.Client.Core.Rooms;
using Lumora.Client.Core.Sync;
using Lumora.Client.Core.Transport;

namespace Lumora.Client.Android;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"));

        RegisterServices(builder.Services);

        return builder.Build();
    }

    /// <summary>
    /// Mirrors Client.Desktop's App.axaml.cs ComposeAndStartAsync — same object graph, same
    /// Client.Core services, just wired through MAUI's DI container instead of manual `new`.
    /// </summary>
    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(_ =>
        {
            var baseAddress = ServerSettings.LoadBaseAddress();
            return new HttpClient { BaseAddress = baseAddress };
        });

        services.AddSingleton<LumoraApiClient>();
        services.AddSingleton<LumoraRealtimeClient>();
        services.AddSingleton<Lumora.Client.Core.Rooms.ISecureStorage, MauiSecureStorage>();
        services.AddSingleton<ActiveRoomStore>();
        services.AddSingleton<IDeviceIdentity, AndroidDeviceIdentity>();

        services.AddSingleton(_ => new AndroidClipboardBridge(global::Android.App.Application.Context));
        services.AddSingleton<Lumora.Client.Core.Clipboard.IClipboardBridge>(
            sp => sp.GetRequiredService<AndroidClipboardBridge>());

        services.AddSingleton<ClipboardSyncEngine>();

        services.AddSingleton(sp => new RoomSessionService(
            sp.GetRequiredService<LumoraApiClient>(),
            sp.GetRequiredService<LumoraRealtimeClient>(),
            sp.GetRequiredService<ActiveRoomStore>(),
            sp.GetRequiredService<ClipboardSyncEngine>(),
            sp.GetRequiredService<IDeviceIdentity>(),
            ServerSettings.HubUri(sp.GetRequiredService<HttpClient>().BaseAddress!)));

        services.AddSingleton<AppShell>();
        services.AddTransient<ClipboardPage>();
        services.AddTransient<DrivePage>();
        services.AddTransient<RoomsPage>();
        services.AddTransient<SettingsPage>();
    }
}
