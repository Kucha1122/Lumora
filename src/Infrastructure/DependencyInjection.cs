using Lumora.Server.Application.Abstractions;
using Lumora.Server.Application.Abstractions.Persistence;
using Lumora.Server.Infrastructure.Auth;
using Lumora.Server.Infrastructure.Persistence;
using Lumora.Server.Infrastructure.Persistence.Repositories;
using Lumora.Server.Infrastructure.Realtime;
using Lumora.Server.Infrastructure.Storage;
using Lumora.Server.Infrastructure.Updates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lumora.Server.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LumoraDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Lumora")));

        services.Configure<BlobStoreOptions>(configuration.GetSection("BlobStore"));
        services.Configure<RoomAuthOptions>(configuration.GetSection("RoomAuth"));
        services.Configure<UpdatePublishOptions>(configuration.GetSection("UpdatePublish"));

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IClipboardRepository, ClipboardRepository>();
        services.AddScoped<IDriveRepository, DriveRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IUpdateReleaseRepository, UpdateReleaseRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IBlobStore, LocalFileSystemBlobStore>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IRoomAuth, RoomAuth>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

        return services;
    }
}
