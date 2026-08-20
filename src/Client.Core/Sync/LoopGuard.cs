using System.Security.Cryptography;

namespace Lumora.Client.Core.Sync;

/// <summary>
/// Suppresses exactly one local clipboard-change notification after this client writes
/// content into the OS clipboard itself. Without this, two synced devices reflect the same
/// content back and forth: A copies → pushed to room → B writes to its clipboard → B's OS
/// fires a change event → B pushes it back to the room → A writes it again → ...
/// </summary>
public sealed class LoopGuard
{
    private byte[]? suppressedHash;

    public void SuppressNext(byte[] plaintextContent)
    {
        suppressedHash = SHA256.HashData(plaintextContent);
    }

    /// <summary>Returns true if this content change originated from our own <see cref="SuppressNext"/> call.</summary>
    public bool ShouldIgnore(byte[] plaintextContent)
    {
        if (suppressedHash is null)
        {
            return false;
        }

        var isMatch = CryptographicOperations.FixedTimeEquals(suppressedHash, SHA256.HashData(plaintextContent));
        if (isMatch)
        {
            suppressedHash = null;
        }

        return isMatch;
    }
}
