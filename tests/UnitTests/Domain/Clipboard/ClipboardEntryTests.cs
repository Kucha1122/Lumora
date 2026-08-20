using FluentAssertions;
using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.UnitTests.Domain.Clipboard;

public class ClipboardEntryTests
{
    [Fact]
    public void CreateInline_PayloadPrzekraczaProgInline_ZwracaFailure()
    {
        var payload = EncryptedPayload.Create(new byte[ClipboardEntry.InlineThresholdBytes + 1]).Value!;

        var result = ClipboardEntry.CreateInline(
            Guid.NewGuid(), ClipboardEntryKind.Text, payload, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreateInline_PoprawnyPayload_ZwracaSuccess()
    {
        var payload = EncryptedPayload.Create(new byte[28]).Value!;

        var result = ClipboardEntry.CreateInline(
            Guid.NewGuid(), ClipboardEntryKind.Text, payload, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CreateFromBlob_RozmiarZeroLubUjemny_ZwracaFailure()
    {
        var result = ClipboardEntry.CreateFromBlob(
            Guid.NewGuid(), ClipboardEntryKind.Image, BlobId.New(), 0, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreateFromBlob_PoprawnyRozmiar_ZwracaSuccess()
    {
        var result = ClipboardEntry.CreateFromBlob(
            Guid.NewGuid(), ClipboardEntryKind.Image, BlobId.New(), 1024, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }
}
