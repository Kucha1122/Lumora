using FluentAssertions;
using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Application.Rooms;
using Lumora.Server.Domain.Devices;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using NSubstitute;

namespace Lumora.UnitTests.Application.Rooms;

public class JoinRoomHandlerTests
{
    private readonly IRoomRepository rooms = Substitute.For<IRoomRepository>();
    private readonly IDeviceRepository devices = Substitute.For<IDeviceRepository>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRoomAuth roomAuth = Substitute.For<IRoomAuth>();
    private readonly IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();

    private JoinRoomHandler CreateHandler() => new(rooms, devices, unitOfWork, roomAuth, clock);

    public JoinRoomHandlerTests()
    {
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_PokojNieIstniejeIZlaSlugPrywatnego_ZwracajaTenSamKomunikatBledu()
    {
        // Nieistniejący pokój
        rooms.GetBySlugAsync(Arg.Any<RoomSlug>(), Arg.Any<CancellationToken>())
            .Returns((Room?)null);
        var handlerForMissingRoom = CreateHandler();
        var missingRoomResult = await handlerForMissingRoom.Handle(
            new JoinRoomCommand("nieistniejacy-pokoj", [1, 2, 3], "Laptop", "Windows"), CancellationToken.None);

        // Istniejący prywatny pokój, złe hasło
        var privateRoom = Room.CreatePrivate(
            RoomSlug.Create("private-room").Value!, "Nazwa", [1], [9, 9], DateTimeOffset.UtcNow).Value!;
        rooms.GetBySlugAsync(Arg.Any<RoomSlug>(), Arg.Any<CancellationToken>())
            .Returns(privateRoom);
        roomAuth.VerifyAuthKey(Arg.Any<byte[]>(), Arg.Any<byte[]>()).Returns(false);
        var handlerForWrongPassword = CreateHandler();
        var wrongPasswordResult = await handlerForWrongPassword.Handle(
            new JoinRoomCommand("private-room", [1, 2, 3], "Laptop", "Windows"), CancellationToken.None);

        missingRoomResult.IsSuccess.Should().BeFalse();
        wrongPasswordResult.IsSuccess.Should().BeFalse();
        wrongPasswordResult.Error.Should().Be(missingRoomResult.Error);
    }

    [Fact]
    public async Task Handle_DolaczenieDoPokojuPublicznego_KonczySieSukcesemBezAuthKey()
    {
        var publicRoom = Room.CreatePublic(RoomSlug.Create("public-room").Value!, "Nazwa", DateTimeOffset.UtcNow).Value!;
        rooms.GetBySlugAsync(Arg.Any<RoomSlug>(), Arg.Any<CancellationToken>()).Returns(publicRoom);
        roomAuth.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("token");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new JoinRoomCommand("public-room", null, "Laptop", "Windows"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DolaczenieDoPokojuPrywatnegoZPoprawnymAuthKey_WydajeTokenIDodajeUrzadzenie()
    {
        var privateRoom = Room.CreatePrivate(
            RoomSlug.Create("private-room").Value!, "Nazwa", [1], [9, 9], DateTimeOffset.UtcNow).Value!;
        rooms.GetBySlugAsync(Arg.Any<RoomSlug>(), Arg.Any<CancellationToken>()).Returns(privateRoom);
        roomAuth.VerifyAuthKey(Arg.Any<byte[]>(), Arg.Any<byte[]>()).Returns(true);
        roomAuth.IssueAccessToken(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("token");
        var handler = CreateHandler();

        var result = await handler.Handle(
            new JoinRoomCommand("private-room", [1, 2, 3], "Laptop", "Windows"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        roomAuth.Received(1).IssueAccessToken(privateRoom.Id, Arg.Any<Guid>());
        devices.Received(1).Add(Arg.Any<Device>());
    }

    [Fact]
    public async Task Handle_BrakAuthKeyDlaPokojuPrywatnego_ZwracaFailure()
    {
        var privateRoom = Room.CreatePrivate(
            RoomSlug.Create("private-room").Value!, "Nazwa", [1], [9, 9], DateTimeOffset.UtcNow).Value!;
        rooms.GetBySlugAsync(Arg.Any<RoomSlug>(), Arg.Any<CancellationToken>()).Returns(privateRoom);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new JoinRoomCommand("private-room", null, "Laptop", "Windows"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
