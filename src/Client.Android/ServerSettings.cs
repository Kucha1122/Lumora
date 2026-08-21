using Microsoft.Maui.Storage;

namespace Lumora.Client.Android;

/// <summary>Android equivalent of Client.Desktop's appsettings.json-backed server address —
/// stored in Preferences instead so it's changeable from the Ustawienia page without a rebuild.</summary>
public static class ServerSettings
{
    private const string Key = "lumora-server-base-address";

    // Same Tailscale address Client.Desktop defaults to (src/Client.Desktop/appsettings.json).
    private const string Default = "https://k3s-server.tail11891a.ts.net/lumora-api/";

    public static Uri LoadBaseAddress()
    {
        var address = Preferences.Default.Get(Key, Default);
        return new Uri(Normalize(address));
    }

    public static void SaveBaseAddress(string address) =>
        Preferences.Default.Set(Key, Normalize(address));

    /// <summary>Must end with "/" — HttpClient and the SignalR hub URI merge relative paths
    /// against BaseAddress, and without a trailing slash a reverse-proxy prefix like
    /// "/lumora-api" would be dropped as if it were a filename (see LumoraApiClient.cs).</summary>
    private static string Normalize(string address) => address.EndsWith('/') ? address : address + "/";

    public static Uri HubUri(Uri serverBaseAddress) => new(serverBaseAddress, "hubs/room");
}
