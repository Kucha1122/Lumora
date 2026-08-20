using FluentAssertions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Rooms;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Rooms;

public class ListRoomsHandlerTests
{
    private readonly IRoomRepository rooms = Substitute.For<IRoomRepository>();

    private ListRoomsHandler CreateHandler() => new(rooms);

    [Fact]
    public async Task Handle_MieszankaPokoiPublicznychIPrywatnych_MapujeSlugNazweIWidocznosc()
    {
        var publicRoom = Room.CreatePublic(RoomSlug.Create("alpha-room").Value!, "Alpha", DateTimeOffset.UtcNow).Value!;
        var privateRoom = Room.CreatePrivate(
            RoomSlug.Create("beta-room").Value!, "Beta", [1, 2], [3, 4], DateTimeOffset.UtcNow).Value!;
        rooms.ListAllAsync(Arg.Any<CancellationToken>()).Returns([publicRoom, privateRoom]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ListRoomsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(
        [
            new RoomSummary("alpha-room", "Alpha", false),
            new RoomSummary("beta-room", "Beta", true)
        ]);
    }

    [Fact]
    public async Task Handle_PokojePrzekazaneWNiealfabetycznejKolejnosci_SortujePoNazwieBezWzgleduNaWielkoscLiter()
    {
        var zetaRoom = Room.CreatePublic(RoomSlug.Create("zeta-room").Value!, "zeta", DateTimeOffset.UtcNow).Value!;
        var alphaRoom = Room.CreatePublic(RoomSlug.Create("alpha-room").Value!, "Alpha", DateTimeOffset.UtcNow).Value!;
        var middleRoom = Room.CreatePublic(RoomSlug.Create("middle-room").Value!, "Middle", DateTimeOffset.UtcNow).Value!;
        rooms.ListAllAsync(Arg.Any<CancellationToken>()).Returns([zetaRoom, alphaRoom, middleRoom]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ListRoomsQuery(), CancellationToken.None);

        result.Select(r => r.DisplayName).Should().ContainInOrder("Alpha", "Middle", "zeta");
    }

    [Fact]
    public async Task Handle_BrakPokoi_ZwracaPustaListe()
    {
        rooms.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ListRoomsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
