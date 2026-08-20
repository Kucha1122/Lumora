using FluentAssertions;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.UnitTests.Domain.ValueObjects;

public class EncryptedPayloadTests
{
    [Fact]
    public void Create_PayloadPusty_ZwracaFailure()
    {
        var bytes = Array.Empty<byte>();

        var result = EncryptedPayload.Create(bytes);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_PayloadNull_ZwracaFailure()
    {
        var result = EncryptedPayload.Create(null);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_PayloadJedenBajt_ZwracaSuccess()
    {
        // A public room's content is plaintext, not AEAD-wrapped — Domain must not assume
        // a minimum ciphertext length. See EncryptedPayload's class doc comment.
        var bytes = new byte[] { 1 };

        var result = EncryptedPayload.Create(bytes);

        result.IsSuccess.Should().BeTrue();
    }
}
