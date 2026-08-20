using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Clipboard;
using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Clipboard;

public class DeleteClipboardEntryHandlerTests
{
    private readonly IClipboardRepository clipboard = Substitute.For<IClipboardRepository>();
    private readonly IBlobStore blobStore = Substitute.For<IBlobStore>();
    private readonly IRealtimeNotifier realtime = Substitute.For<IRealtimeNotifier>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteClipboardEntryHandler CreateHandler() => new(clipboard, blobStore, realtime, unitOfWork);

    [Fact]
    public async Task Handle_WpisInline_UsuwaZapisujeIPowiadamiaBezKasowaniaBloba()
    {
        var roomId = Guid.NewGuid();
        var entry = ClipboardEntry.CreateInline(
            roomId, ClipboardEntryKind.Text, EncryptedPayload.Create(new byte[16]).Value!, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        clipboard.GetByIdAsync(roomId, entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var handler = CreateHandler();
        var command = new DeleteClipboardEntryCommand(roomId, entry.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clipboard.Received(1).Remove(entry);
        await blobStore.DidNotReceiveWithAnyArgs().DeleteAsync(default, default!, default);
        await realtime.Received(1).ClipboardEntryDeletedAsync(roomId, entry.Id, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WpisZBlobem_KasujeBlobWStore()
    {
        var roomId = Guid.NewGuid();
        var entry = ClipboardEntry.CreateFromBlob(
            roomId, ClipboardEntryKind.Image, BlobId.New(), 2048, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        clipboard.GetByIdAsync(roomId, entry.Id, Arg.Any<CancellationToken>()).Returns(entry);
        var handler = CreateHandler();
        var command = new DeleteClipboardEntryCommand(roomId, entry.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await blobStore.Received(1).DeleteAsync(roomId, entry.BlobId!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WpisNieIstnieje_ZwracaFailureBezUsuwaniaIPowiadamiania()
    {
        var roomId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        clipboard.GetByIdAsync(roomId, entryId, Arg.Any<CancellationToken>()).Returns((ClipboardEntry?)null);
        var handler = CreateHandler();
        var command = new DeleteClipboardEntryCommand(roomId, entryId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        clipboard.DidNotReceiveWithAnyArgs().Remove(default!);
        await blobStore.DidNotReceiveWithAnyArgs().DeleteAsync(default, default!, default);
        await realtime.DidNotReceiveWithAnyArgs().ClipboardEntryDeletedAsync(default, default, default);
    }
}
