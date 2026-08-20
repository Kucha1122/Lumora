using System.Text;
using System.Text.Json;
using Lumora.Client.Core.Crypto;

namespace Lumora.Client.Core.Drive;

/// <summary>
/// The filename and MIME type, JSON-encoded then AES-GCM encrypted client-side — the server
/// only ever stores/returns the resulting bytes as DriveFile.EncryptedMetadata.
/// </summary>
public sealed record DriveFileMetadata(string FileName, string MimeType)
{
    public byte[] EncryptFor(byte[]? encKey)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(this);
        return encKey is null ? json : PayloadCipher.Encrypt(encKey, json);
    }

    public static DriveFileMetadata DecryptFrom(byte[] encryptedMetadata, byte[]? encKey)
    {
        var json = encKey is null ? encryptedMetadata : PayloadCipher.Decrypt(encKey, encryptedMetadata);
        return JsonSerializer.Deserialize<DriveFileMetadata>(json)
            ?? throw new InvalidOperationException("Nie udało się zdekodować metadanych pliku.");
    }
}
