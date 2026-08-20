using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Lumora.Client.Core.Crypto;

/// <summary>
/// masterKey = Argon2id(password, salt, m=64MB, t=3, p=4) → 32B
/// authKey   = HKDF-SHA256(masterKey, info: "lumora-auth-v1") → sent to the server
/// encKey    = HKDF-SHA256(masterKey, info: "lumora-enc-v1")  → stays on this device
///
/// Argon2id runs once per password entry (~200-500ms by design — this is the only barrier
/// against offline brute-forcing of a private room's password); HKDF is cheap and only
/// separates the single master secret into two keys with distinct purposes.
/// </summary>
public static class RoomKeyDerivation
{
    public const int SaltLength = 16;
    private const int MasterKeyLength = 32;
    private const int DerivedKeyLength = 32;

    private const int MemorySizeKb = 64 * 1024;
    private const int Iterations = 3;
    private const int DegreeOfParallelism = 4;

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltLength);

    public static RoomKeyMaterial Derive(string password, byte[] kdfSalt)
    {
        var masterKey = DeriveMasterKey(password, kdfSalt);
        try
        {
            var authKey = HKDF.Expand(HashAlgorithmName.SHA256, masterKey, DerivedKeyLength, InfoBytes("lumora-auth-v1"));
            var encKey = HKDF.Expand(HashAlgorithmName.SHA256, masterKey, DerivedKeyLength, InfoBytes("lumora-enc-v1"));
            return new RoomKeyMaterial(authKey, encKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(masterKey);
        }
    }

    private static byte[] DeriveMasterKey(string password, byte[] kdfSalt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = kdfSalt,
                DegreeOfParallelism = DegreeOfParallelism,
                Iterations = Iterations,
                MemorySize = MemorySizeKb
            };

            return argon2.GetBytes(MasterKeyLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static byte[] InfoBytes(string info) => Encoding.UTF8.GetBytes(info);
}
