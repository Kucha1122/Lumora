namespace Lumora.Server.Application.Updates;

/// <summary>
/// IBlobStore is keyed by roomId, but APK releases have no room — they reuse the same store
/// under this fixed sentinel "room" instead of introducing a second blob storage abstraction
/// for a single file type. Never used as a real room id anywhere else.
/// </summary>
public static class UpdateBlobs
{
    public static readonly Guid Namespace = Guid.Parse("00000000-0000-0000-0000-00000000a9dd");
}
