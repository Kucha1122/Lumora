using Lumora.Server.Domain.Common;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.Server.Domain.Rooms;

/// <summary>
/// A room has no owner — knowing its slug and (for private rooms) its password
/// grants full access. There are no user accounts in this system.
/// </summary>
public sealed class Room
{
    public Guid Id { get; }
    public RoomSlug Slug { get; }
    public string DisplayName { get; }
    public RoomVisibility Visibility { get; }

    /// <summary>Public salt used by clients to derive the room's master key from its password.</summary>
    public byte[]? KdfSalt { get; }

    /// <summary>SHA-256(authKey || serverSalt) — never the password or the encryption key itself.</summary>
    public byte[]? AuthKeyHash { get; }

    public DateTimeOffset CreatedAt { get; }

    private Room(
        Guid id,
        RoomSlug slug,
        string displayName,
        RoomVisibility visibility,
        byte[]? kdfSalt,
        byte[]? authKeyHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        Slug = slug;
        DisplayName = displayName;
        Visibility = visibility;
        KdfSalt = kdfSalt;
        AuthKeyHash = authKeyHash;
        CreatedAt = createdAt;
    }

    public static Result<Room> CreatePublic(RoomSlug slug, string displayName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<Room>.Failure("Nazwa przestrzeni nie może być pusta.");
        }

        return Result<Room>.Success(
            new Room(Guid.NewGuid(), slug, displayName, RoomVisibility.Public, null, null, now));
    }

    public static Result<Room> CreatePrivate(
        RoomSlug slug, string displayName, byte[] kdfSalt, byte[] authKeyHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<Room>.Failure("Nazwa przestrzeni nie może być pusta.");
        }

        if (kdfSalt is not { Length: > 0 })
        {
            return Result<Room>.Failure("Prywatna przestrzeń wymaga soli KDF.");
        }

        if (authKeyHash is not { Length: > 0 })
        {
            return Result<Room>.Failure("Prywatna przestrzeń wymaga hasha klucza uwierzytelniającego.");
        }

        return Result<Room>.Success(
            new Room(Guid.NewGuid(), slug, displayName, RoomVisibility.Private, kdfSalt, authKeyHash, now));
    }
}
