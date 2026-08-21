using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Updates;
using Lumora.Server.Domain.Updates;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Updates;

public class PublishAndroidReleaseHandlerTests
{
    private readonly IUpdateReleaseRepository releases = Substitute.For<IUpdateReleaseRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();

    private PublishAndroidReleaseHandler CreateHandler() => new(releases, unitOfWork, clock);

    public PublishAndroidReleaseHandlerTests()
    {
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        releases.GetLatestAsync(Arg.Any<CancellationToken>()).Returns((UpdateRelease?)null);
    }

    [Fact]
    public async Task Handle_PierwszaPublikacja_ZwracaSuccess()
    {
        var handler = CreateHandler();
        var command = new PublishAndroidReleaseCommand("1.0.0", 1, Guid.NewGuid(), 1024, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        releases.Received(1).Add(Arg.Is<UpdateRelease>(r => r.VersionCode == 1));
    }

    [Fact]
    public async Task Handle_VersionCodeNieWyzszyNizOpublikowany_ZwracaFailure()
    {
        var existing = UpdateRelease.Create("1.0.0", 5, BlobId.New(), 1024, null, DateTimeOffset.UtcNow).Value!;
        releases.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(existing);
        var handler = CreateHandler();
        var command = new PublishAndroidReleaseCommand("1.0.1", 5, Guid.NewGuid(), 2048, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        releases.DidNotReceive().Add(Arg.Any<UpdateRelease>());
    }

    [Fact]
    public async Task Handle_VersionCodeWyzszyNizOpublikowany_ZwracaSuccess()
    {
        var existing = UpdateRelease.Create("1.0.0", 5, BlobId.New(), 1024, null, DateTimeOffset.UtcNow).Value!;
        releases.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(existing);
        var handler = CreateHandler();
        var command = new PublishAndroidReleaseCommand("1.1.0", 6, Guid.NewGuid(), 2048, "Notatki");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VersionCode.Should().Be(6);
    }

    [Fact]
    public async Task Handle_UjemnyRozmiarPliku_ZwracaFailure()
    {
        var handler = CreateHandler();
        var command = new PublishAndroidReleaseCommand("1.0.0", 1, Guid.NewGuid(), 0, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
