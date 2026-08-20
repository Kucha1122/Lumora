using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Rooms;
using Lumora.Server.Domain.Rooms;
using NSubstitute;

namespace Lumora.UnitTests.Application.Rooms;

public class CreateRoomHandlerTests
{
    private readonly IRoomRepository rooms = Substitute.For<IRoomRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRoomAuth roomAuth = Substitute.For<IRoomAuth>();
    private readonly IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();

    private CreateRoomHandler CreateHandler() => new(rooms, unitOfWork, roomAuth, clock);

    public CreateRoomHandlerTests()
    {
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        rooms.SlugExistsAsync(Arg.Any<Lumora.Server.Domain.ValueObjects.RoomSlug>(), Arg.Any<CancellationToken>())
            .Returns(false);
    }

    [Fact]
    public async Task Handle_PoprawnaPrzestrzenPubliczna_ZwracaSuccess()
    {
        var handler = CreateHandler();
        var command = new CreateRoomCommand("public-room", "Nazwa", false, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PoprawnaPrzestrzenPrywatna_HashujeKluczIDodajePrywatnaPrzestrzen()
    {
        var handler = CreateHandler();
        var authKey = new byte[] { 1, 2, 3 };
        var authKeyHash = new byte[] { 9, 9, 9 };
        roomAuth.HashAuthKey(authKey).Returns(authKeyHash);
        var command = new CreateRoomCommand("private-room", "Nazwa", true, [1, 2], authKey);

        await handler.Handle(command, CancellationToken.None);

        roomAuth.Received(1).HashAuthKey(authKey);
        rooms.Received(1).Add(Arg.Is<Room>(r => r.Visibility == RoomVisibility.Private));
    }

    [Fact]
    public async Task Handle_SlugJuzIstnieje_ZwracaFailure()
    {
        rooms.SlugExistsAsync(Arg.Any<Lumora.Server.Domain.ValueObjects.RoomSlug>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateHandler();
        var command = new CreateRoomCommand("existing-room", "Nazwa", false, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PrywatnaBezSoliIKlucza_ZwracaFailure()
    {
        var handler = CreateHandler();
        var command = new CreateRoomCommand("private-room", "Nazwa", true, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
