namespace Lumora.Server.Application.Abstractions;

/// <summary>
/// Server-side half of the E2E auth scheme. The server never sees a password or the
/// encryption key — only the client-derived authKey, which it hashes and compares.
/// </summary>
public interface IRoomAuth
{
    /// <summary>
    /// Deterministic salt for a slug that has no room, derived from a server-side pepper.
    /// Makes "unknown slug" and "existing room" indistinguishable to a salt lookup.
    /// </summary>
    byte[] DeriveFakeSalt(string slug);

    /// <summary>Deterministic stand-in for AuthKeyHash when no room exists, used so a
    /// join attempt against an unknown slug costs the same as one against a real room.</summary>
    byte[] DeriveFakeAuthKeyHash(string slug);

    byte[] HashAuthKey(byte[] authKey);

    bool VerifyAuthKey(byte[] authKey, byte[] expectedHash);

    /// <summary>Issues a short-lived JWT scoped to a single room and device.</summary>
    string IssueAccessToken(Guid roomId, Guid deviceId);
}
