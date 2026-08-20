using Lumora.Server.Domain.Common;

namespace Lumora.Server.Domain.ValueObjects;

/// <summary>
/// Opaque content produced client-side — AES-256-GCM ciphertext (nonce || ciphertext || tag)
/// for a private room, or plaintext bytes as-is for the public room (see plan §Model
/// bezpieczeństwa: public rooms are deliberately jawny/plaintext). The server never
/// interprets this — it only stores and returns the bytes — so Domain deliberately doesn't
/// assume an AEAD wire format here; that's a client-side (Client.Core) concern.
/// </summary>
public sealed class EncryptedPayload
{
    public byte[] Bytes { get; }

    private EncryptedPayload(byte[] bytes) => Bytes = bytes;

    public static Result<EncryptedPayload> Create(byte[]? bytes)
    {
        if (bytes is not { Length: > 0 })
        {
            return Result<EncryptedPayload>.Failure("Payload nie może być pusty.");
        }

        return Result<EncryptedPayload>.Success(new EncryptedPayload(bytes));
    }

    public int SizeBytes => Bytes.Length;

    /// <summary>Reconstructs a payload already validated once (persistence round-trip only).</summary>
    internal static EncryptedPayload FromTrusted(byte[] bytes) => new(bytes);
}
