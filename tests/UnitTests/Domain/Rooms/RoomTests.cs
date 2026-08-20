using FluentAssertions;
using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.UnitTests.Domain.Rooms;

public class RoomTests
{
    private static readonly RoomSlug Slug = RoomSlug.Create("test-room").Value!;

    [Fact]
    public void CreatePublic_PustaNazwa_ZwracaFailure()
    {
        var result = Room.CreatePublic(Slug, " ", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreatePublic_PoprawneDane_ZwracaSuccessZWidocznosciaPublic()
    {
        var result = Room.CreatePublic(Slug, "Moja przestrzeń", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Visibility.Should().Be(RoomVisibility.Public);
    }

    [Fact]
    public void CreatePrivate_PustaNazwa_ZwracaFailure()
    {
        var result = Room.CreatePrivate(Slug, " ", [1], [1], DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreatePrivate_PustaSol_ZwracaFailure()
    {
        var result = Room.CreatePrivate(Slug, "Nazwa", [], [1], DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreatePrivate_PustyAuthKeyHash_ZwracaFailure()
    {
        var result = Room.CreatePrivate(Slug, "Nazwa", [1], [], DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreatePrivate_PoprawneDane_ZwracaSuccessZWidocznosciaPrivate()
    {
        var result = Room.CreatePrivate(Slug, "Nazwa", [1, 2], [3, 4], DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Visibility.Should().Be(RoomVisibility.Private);
    }
}
