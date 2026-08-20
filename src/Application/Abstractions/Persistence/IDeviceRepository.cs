using Lumora.Server.Domain.Devices;

namespace Lumora.Server.Application.Abstractions.Persistence;

public interface IDeviceRepository
{
    Task<Device?> GetByIdAsync(Guid id, CancellationToken ct);

    void Add(Device device);
}
