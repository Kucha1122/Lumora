namespace Lumora.Client.Core.Rooms;

/// <summary>Stable per-install device identity. Implemented per-platform in the client shell
/// (e.g. DPAPI-backed file on Windows, SecureStorage on Android).</summary>
public interface IDeviceIdentity
{
    Guid Id { get; }

    string DisplayName { get; }

    string Platform { get; }
}
