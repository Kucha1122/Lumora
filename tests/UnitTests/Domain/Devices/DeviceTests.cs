using FluentAssertions;
using Lumora.Server.Domain.Devices;

namespace Lumora.UnitTests.Domain.Devices;

public class DeviceTests
{
    [Fact]
    public void Register_PustaNazwaWyswietlana_ZwracaFailure()
    {
        var result = Device.Register(Guid.NewGuid(), " ", "Windows", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Register_PustaPlatforma_ZwracaFailure()
    {
        var result = Device.Register(Guid.NewGuid(), "Laptop", " ", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Register_PoprawneDane_ZwracaSuccess()
    {
        var result = Device.Register(Guid.NewGuid(), "Laptop", "Windows", DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Touch_WywolanaZNowaData_AktualizujeLastSeenAt()
    {
        var device = Device.Register(Guid.NewGuid(), "Laptop", "Windows", DateTimeOffset.UtcNow).Value!;
        var newTimestamp = DateTimeOffset.UtcNow.AddMinutes(5);

        device.Touch(newTimestamp);

        device.LastSeenAt.Should().Be(newTimestamp);
    }
}
