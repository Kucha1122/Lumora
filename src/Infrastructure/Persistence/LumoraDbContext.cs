using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.Devices;
using Lumora.Server.Domain.Drive;
using Lumora.Server.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Lumora.Server.Infrastructure.Persistence;

public sealed class LumoraDbContext(DbContextOptions<LumoraDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<ClipboardEntry> ClipboardEntries => Set<ClipboardEntry>();

    public DbSet<DriveFile> DriveFiles => Set<DriveFile>();

    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LumoraDbContext).Assembly);
    }
}
