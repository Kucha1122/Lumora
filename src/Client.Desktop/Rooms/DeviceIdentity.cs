using Lumora.Client.Core.Rooms;

namespace Lumora.Client.Desktop.Rooms;

/// <summary>
/// One stable device id per install, stored alongside the room state. Two Client.Desktop
/// instances on the same machine (e.g. during manual testing) must run with separate
/// %AppData% profiles to get distinct ids — see plan §Weryfikacja, step 2.
/// </summary>
public sealed class DeviceIdentity : IDeviceIdentity
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumora", "device.id");

    public Guid Id { get; } = GetOrCreate();

    public string DisplayName => Environment.MachineName;

    public string Platform => "Windows";

    private static Guid GetOrCreate()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        if (File.Exists(FilePath) && Guid.TryParse(File.ReadAllText(FilePath), out var existing))
        {
            return existing;
        }

        var id = Guid.NewGuid();
        File.WriteAllText(FilePath, id.ToString());
        return id;
    }
}
