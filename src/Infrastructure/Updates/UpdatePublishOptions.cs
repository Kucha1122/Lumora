namespace Lumora.Server.Infrastructure.Updates;

/// <summary>Shared secret gating POST /updates/android* — a CI pipeline concern, unrelated
/// to room JWTs, so it lives outside RoomAuth. See UpdatePublishSecretFilter.</summary>
public sealed class UpdatePublishOptions
{
    public required string Secret { get; init; }
}
