using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Lumora.Server.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository(LumoraDbContext db) : IDeviceRepository
{
    public Task<Device?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);

    public void Add(Device device) => db.Devices.Add(device);
}
