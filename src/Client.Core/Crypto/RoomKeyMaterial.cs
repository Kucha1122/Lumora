namespace Lumora.Client.Core.Crypto;

/// <summary>
/// Keys derived client-side from a room password. <see cref="EncKey"/> never leaves this
/// process — it is not serialized, logged, or sent over the wire. Only <see cref="AuthKey"/>
/// (itself already high-entropy, not the password) is sent to the server to prove membership.
/// </summary>
public sealed class RoomKeyMaterial
{
    public byte[] AuthKey { get; }
    public byte[] EncKey { get; }

    public RoomKeyMaterial(byte[] authKey, byte[] encKey)
    {
        AuthKey = authKey;
        EncKey = encKey;
    }
}
