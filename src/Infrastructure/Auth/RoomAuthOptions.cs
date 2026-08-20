namespace Lumora.Server.Infrastructure.Auth;

public sealed class RoomAuthOptions
{
    /// <summary>Server-side secret used to derive fake salts/hashes for unknown slugs and to sign JWTs.</summary>
    public required string Pepper { get; init; }

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromHours(12);
}
