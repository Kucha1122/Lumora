using Lumora.Client.Core.Rooms;
using Microsoft.Maui.Storage;

namespace Lumora.Client.Android.Rooms;

/// <summary>One stable device id per install, generated once and kept in Preferences
/// (unlike RoomProfile keys, the id itself isn't sensitive — no need for SecureStorage).</summary>
public sealed class AndroidDeviceIdentity : IDeviceIdentity
{
    private const string IdKey = "lumora-device-id";

    public Guid Id { get; } = GetOrCreate();

    public string DisplayName => global::Android.OS.Build.Model ?? "Android";

    public string Platform => "Android";

    private static Guid GetOrCreate()
    {
        var stored = Preferences.Default.Get(IdKey, string.Empty);
        if (Guid.TryParse(stored, out var existing))
        {
            return existing;
        }

        var id = Guid.NewGuid();
        Preferences.Default.Set(IdKey, id.ToString());
        return id;
    }
}
