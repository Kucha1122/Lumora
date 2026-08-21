using FluentAssertions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Updates;
using Lumora.Server.Domain.Updates;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Updates;

public class GetLatestAndroidReleaseHandlerTests
{
    private readonly IUpdateReleaseRepository releases = Substitute.For<IUpdateReleaseRepository>();

    private GetLatestAndroidReleaseHandler CreateHandler() => new(releases);

    [Fact]
    public async Task Handle_BrakOpublikowanychWersji_ZwracaNull()
    {
        releases.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((UpdateRelease?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetLatestAndroidReleaseQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_IstniejeOpublikowanaWersja_ZwracaJejDane()
    {
        var release = UpdateRelease.Create("1.2.3", 7, BlobId.New(), 4096, "Notatki", DateTimeOffset.UtcNow).Value!;
        releases.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(release);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetLatestAndroidReleaseQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Version.Should().Be("1.2.3");
        result.VersionCode.Should().Be(7);
        result.BlobId.Should().Be(release.BlobId.Value);
    }
}
