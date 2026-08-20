using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Clipboard;
using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Clipboard;

public class ClearClipboardHandlerTests
{
    private readonly IClipboardRepository clipboard = Substitute.For<IClipboardRepository>();
    private readonly IBlobStore blobStore = Substitute.For<IBlobStore>();
    private readonly IRealtimeNotifier realtime = Substitute.For<IRealtimeNotifier>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

    private ClearClipboardHandler CreateHandler() => new(clipboard, blobStore, realtime, unitOfWork);

    [Fact]
    public async Task Handle_MieszankaWpisowInlineIZBlobami_UsuwaWszystkieIKasujeTylkoBloby()
    {
        var roomId = Guid.NewGuid();
        var inlineEntry = ClipboardEntry.CreateInline(
            roomId, ClipboardEntryKind.Text, EncryptedPayload.Create(new byte[16]).Value!, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        var blobEntry = ClipboardEntry.CreateFromBlob(
            roomId, ClipboardEntryKind.Image, BlobId.New(), 2048, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        clipboard.ListAllAsync(roomId, Arg.Any<CancellationToken>()).Returns([inlineEntry, blobEntry]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ClearClipboardCommand(roomId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clipboard.Received(1).Remove(inlineEntry);
        clipboard.Received(1).Remove(blobEntry);
        await blobStore.Received(1).DeleteAsync(roomId, blobEntry.BlobId!, Arg.Any<CancellationToken>());
        await blobStore.Received(1).DeleteAsync(Arg.Any<Guid>(), Arg.Any<BlobId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MieszankaWpisow_ZapisujeIPowiadamiaDokladnieRaz()
    {
        var roomId = Guid.NewGuid();
        var inlineEntry = ClipboardEntry.CreateInline(
            roomId, ClipboardEntryKind.Text, EncryptedPayload.Create(new byte[16]).Value!, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        clipboard.ListAllAsync(roomId, Arg.Any<CancellationToken>()).Returns([inlineEntry]);
        var handler = CreateHandler();

        await handler.Handle(new ClearClipboardCommand(roomId), CancellationToken.None);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await realtime.Received(1).ClipboardClearedAsync(roomId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PustyPokoj_ZwracaSuccessIWciazPowiadamiaBezUsuwania()
    {
        var roomId = Guid.NewGuid();
        clipboard.ListAllAsync(roomId, Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ClearClipboardCommand(roomId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clipboard.DidNotReceiveWithAnyArgs().Remove(default!);
        await blobStore.DidNotReceiveWithAnyArgs().DeleteAsync(default, default!, default);
        await realtime.Received(1).ClipboardClearedAsync(roomId, Arg.Any<CancellationToken>());
    }
}
