using FluentAssertions;
using Lumora.Server.Domain.ValueObjects;

namespace Lumora.UnitTests.Domain.ValueObjects;

public class RoomSlugTests
{
    [Fact]
    public void Create_PustySlug_ZwracaFailure()
    {
        var result = RoomSlug.Create("");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_SlugZaDlugi_ZwracaFailure()
    {
        var candidate = new string('a', RoomSlug.MaxLength + 1);

        var result = RoomSlug.Create(candidate);

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData("Wielkie-Litery")]
    [InlineData("-leading-hyphen")]
    [InlineData("trailing-hyphen-")]
    [InlineData("spacja tutaj")]
    public void Create_NieprawidloweZnaki_ZwracaFailure(string candidate)
    {
        var result = RoomSlug.Create(candidate);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_PoprawnySlug_ZwracaSuccess()
    {
        var result = RoomSlug.Create("my-room-42");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("my-room-42");
    }
}
