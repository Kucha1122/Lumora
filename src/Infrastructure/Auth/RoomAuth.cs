using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lumora.Server.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Lumora.Server.Infrastructure.Auth;

/// <summary>
/// Server-side half of the E2E auth scheme — never sees a password or an encryption key.
/// authKey already has full entropy (it's an HKDF output), so a fast HMAC is enough to
/// hash it; Argon2id is only ever run client-side against the human-chosen password.
/// </summary>
public sealed class RoomAuth(IOptions<RoomAuthOptions> options) : IRoomAuth
{
    private readonly byte[] pepper = Encoding.UTF8.GetBytes(options.Value.Pepper);
    private readonly TimeSpan tokenLifetime = options.Value.AccessTokenLifetime;

    public byte[] DeriveFakeSalt(string slug) => HmacTag("fake-salt:" + slug, 16);

    public byte[] DeriveFakeAuthKeyHash(string slug) => HmacTag("fake-authkey-hash:" + slug, 32);

    public byte[] HashAuthKey(byte[] authKey)
    {
        using var hmac = new HMACSHA256(pepper);
        return hmac.ComputeHash(authKey);
    }

    public bool VerifyAuthKey(byte[] authKey, byte[] expectedHash) =>
        CryptographicOperations.FixedTimeEquals(HashAuthKey(authKey), expectedHash);

    public string IssueAccessToken(Guid roomId, Guid deviceId)
    {
        var key = new SymmetricSecurityKey(SigningKeyBytes());
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("roomId", roomId.ToString()),
            new Claim("deviceId", deviceId.ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(tokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Deterministic 256-bit signing key derived from the pepper — not the pepper itself.</summary>
    public byte[] SigningKeyBytes() => HmacTag("jwt-signing-key", 32);

    private byte[] HmacTag(string label, int length)
    {
        using var hmac = new HMACSHA256(pepper);
        var full = hmac.ComputeHash(Encoding.UTF8.GetBytes(label));
        return full[..length];
    }
}
