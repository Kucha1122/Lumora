using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Drive;
using Lumora.Server.Domain.Drive;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Drive;

public class DeleteDriveFileHandlerTests
{
    private readonly IDriveRepository driveFiles = Substitute.For<IDriveRepository>();
    private readonly IBlobStore blobStore = Substitute.For<IBlobStore>();
    private readonly IRealtimeNotifier realtime = Substitute.For<IRealtimeNotifier>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteDriveFileHandler CreateHandler() => new(driveFiles, blobStore, realtime, unitOfWork);

    [Fact]
    public async Task Handle_PlikZnaleziony_UsuwaKasujeBlobIPowiadamia()
    {
        var roomId = Guid.NewGuid();
        var metadata = EncryptedPayload.Create(new byte[28]).Value!;
        var file = DriveFile.Create(roomId, metadata, BlobId.New(), 1024, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        driveFiles.GetByIdAsync(roomId, file.Id, Arg.Any<CancellationToken>()).Returns(file);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteDriveFileCommand(roomId, file.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        driveFiles.Received(1).Remove(file);
        await blobStore.Received(1).DeleteAsync(roomId, file.BlobId, Arg.Any<CancellationToken>());
        await realtime.Received(1).DriveFileDeletedAsync(roomId, file.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PlikNieZnaleziony_ZwracaFailureBezUsuwaniaBloba()
    {
        var roomId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        driveFiles.GetByIdAsync(roomId, fileId, Arg.Any<CancellationToken>()).Returns((DriveFile?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new DeleteDriveFileCommand(roomId, fileId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await blobStore.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<BlobId>(), Arg.Any<CancellationToken>());
    }
}
