using System.Security.Cryptography;

namespace Lumora.Client.Core.Crypto;

/// <summary>
/// AES-256-GCM with a random 96-bit nonce per call. Wire format: nonce(12) || ciphertext || tag(16).
/// This is the only place plaintext clipboard/file content exists outside the OS clipboard/filesystem —
/// everything past this boundary (transport, server, disk) only ever sees the combined output.
/// </summary>
public static class PayloadCipher
{
    private const int NonceLength = 12;
    private const int TagLength = 16;

    public static byte[] Encrypt(byte[] encKey, byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLength];

        using var aesGcm = new AesGcm(encKey, TagLength);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceLength + ciphertext.Length + TagLength];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, NonceLength);
        tag.CopyTo(result, NonceLength + ciphertext.Length);
        return result;
    }

    public static byte[] Decrypt(byte[] encKey, byte[] payload)
    {
        if (payload.Length < NonceLength + TagLength)
        {
            throw new CryptographicException("Payload jest za krótki, by zawierać nonce i tag.");
        }

        var nonce = payload[..NonceLength];
        var tag = payload[^TagLength..];
        var ciphertext = payload[NonceLength..^TagLength];
        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(encKey, TagLength);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
