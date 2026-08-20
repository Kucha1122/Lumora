using Lumora.Server.Domain.Common;

namespace Lumora.Server.Domain.Devices;

public sealed class Device
{
    public Guid Id { get; }
    public Guid RoomId { get; }
    public string DisplayName { get; private set; }
    public string Platform { get; }
    public DateTimeOffset LastSeenAt { get; private set; }

    private Device(Guid id, Guid roomId, string displayName, string platform, DateTimeOffset lastSeenAt)
    {
        Id = id;
        RoomId = roomId;
        DisplayName = displayName;
        Platform = platform;
        LastSeenAt = lastSeenAt;
    }

    public static Result<Device> Register(Guid roomId, string displayName, string platform, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result<Device>.Failure("Nazwa urządzenia nie może być pusta.");
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            return Result<Device>.Failure("Platforma urządzenia nie może być pusta.");
        }

        return Result<Device>.Success(new Device(Guid.NewGuid(), roomId, displayName, platform, now));
    }

    public void Touch(DateTimeOffset now) => LastSeenAt = now;
}
