namespace Lumora.Client.Core.Rooms;

/// <summary>
/// A room this client has successfully joined at least once. <see cref="EncKey"/> and
/// <see cref="AuthKey"/> are present only for private rooms and are what makes this record
/// sensitive — persist it only through <see cref="ISecureStorage"/>, never in plain config.
/// AuthKey is stored (not just EncKey) so the client can silently mint a fresh access token
/// after its short-lived JWT expires, without re-prompting for the password — it carries no
/// more risk than any other bearer session credential, since it's exactly what already
/// crosses the network on every join.
/// </summary>
public sealed record RoomProfile(Guid RoomId, string Slug, string DisplayName, bool IsPrivate, byte[]? EncKey, byte[]? AuthKey);
