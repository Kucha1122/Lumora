using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Clipboard;
using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lumora.UnitTests.Application.Clipboard;

public class PushClipboardEntryHandlerTests
{
    private readonly IClipboardRepository clipboard = Substitute.For<IClipboardRepository>();
    private readonly IBlobStore blobStore = Substitute.For<IBlobStore>();
    private readonly IRealtimeNotifier realtime = Substitute.For<IRealtimeNotifier>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
    private readonly IOptions<ClipboardRetentionOptions> retentionOptions =
        Options.Create(new ClipboardRetentionOptions());

    private PushClipboardEntryHandler CreateHandler() =>
        new(clipboard, blobStore, realtime, unitOfWork, clock, retentionOptions);

    public PushClipboardEntryHandlerTests()
    {
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        clipboard.ListOverflowAsync(
                Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task Handle_PoprawnyPayloadInline_DodajeWpisIZapisujeIPowiadamia()
    {
        var handler = CreateHandler();
        var command = new PushClipboardEntryCommand(
            Guid.NewGuid(), ClipboardEntryKind.Text, new byte[28], null, 28, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clipboard.Received(1).Add(Arg.Any<ClipboardEntry>());
        await realtime.Received(1).ClipboardEntryPushedAsync(command.RoomId, Arg.Any<ClipboardEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PoprawnaReferencjaDoBloba_DodajeWpis()
    {
        var handler = CreateHandler();
        var command = new PushClipboardEntryCommand(
            Guid.NewGuid(), ClipboardEntryKind.Image, null, Guid.NewGuid(), 2048, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clipboard.Received(1).Add(Arg.Any<ClipboardEntry>());
    }

    [Fact]
    public async Task Handle_WpisyPrzekraczajaRetencje_UsuwaJeIKasujeBlobyPowiazanychWpisow()
    {
        var roomId = Guid.NewGuid();
        var inlineOverflow = ClipboardEntry.CreateInline(
            roomId, ClipboardEntryKind.Text, EncryptedPayload.Create(new byte[28]).Value!, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;
        var blobOverflow = ClipboardEntry.CreateFromBlob(
            roomId, ClipboardEntryKind.Image, BlobId.New(), 1024, Guid.NewGuid(), DateTimeOffset.UtcNow).Value!;

        clipboard.ListOverflowAsync(
                Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([inlineOverflow, blobOverflow]);

        var handler = CreateHandler();
        var command = new PushClipboardEntryCommand(
            roomId, ClipboardEntryKind.Text, new byte[28], null, 28, Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);

        clipboard.Received(1).Remove(inlineOverflow);
        clipboard.Received(1).Remove(blobOverflow);
        await blobStore.Received(1).DeleteAsync(roomId, blobOverflow.BlobId!, Arg.Any<CancellationToken>());
        await blobStore.DidNotReceive().DeleteAsync(roomId, Arg.Is<BlobId>(b => b == inlineOverflow.BlobId), Arg.Any<CancellationToken>());
    }
}
