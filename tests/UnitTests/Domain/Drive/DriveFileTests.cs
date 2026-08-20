using FluentAssertions;
using Lumora.Server.Domain.Drive;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.UnitTests.Domain.Drive;

public class DriveFileTests
{
    [Fact]
    public void Create_RozmiarZeroLubUjemny_ZwracaFailure()
    {
        var metadata = EncryptedPayload.Create(new byte[28]).Value!;

        var result = DriveFile.Create(Guid.NewGuid(), metadata, BlobId.New(), 0, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_PoprawnyRozmiar_ZwracaSuccess()
    {
        var metadata = EncryptedPayload.Create(new byte[28]).Value!;

        var result = DriveFile.Create(Guid.NewGuid(), metadata, BlobId.New(), 2048, Guid.NewGuid(), DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }
}
