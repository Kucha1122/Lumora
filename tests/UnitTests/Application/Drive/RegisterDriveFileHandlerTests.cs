using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Drive;
using Lumora.Server.Domain.Drive;
using NSubstitute;

namespace Lumora.UnitTests.Application.Drive;

public class RegisterDriveFileHandlerTests
{
    private readonly IDriveRepository driveFiles = Substitute.For<IDriveRepository>();
    private readonly IRealtimeNotifier realtime = Substitute.For<IRealtimeNotifier>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();

    private RegisterDriveFileHandler CreateHandler() => new(driveFiles, realtime, unitOfWork, clock);

    public RegisterDriveFileHandlerTests()
    {
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_PoprawneDane_RejestrujePlikIPowiadamia()
    {
        var handler = CreateHandler();
        var command = new RegisterDriveFileCommand(
            Guid.NewGuid(), Guid.NewGuid(), new byte[28], 1024, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        driveFiles.Received(1).Add(Arg.Any<DriveFile>());
        await realtime.Received(1).DriveFileAddedAsync(command.RoomId, Arg.Any<DriveFile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PusteEncryptedMetadata_ZwracaFailure()
    {
        var handler = CreateHandler();
        var command = new RegisterDriveFileCommand(
            Guid.NewGuid(), Guid.NewGuid(), [], 1024, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        driveFiles.DidNotReceive().Add(Arg.Any<DriveFile>());
    }
}
