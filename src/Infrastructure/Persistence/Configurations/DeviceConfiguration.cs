using Lumora.Server.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumora.Server.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.RoomId).IsRequired();
        builder.HasIndex(d => d.RoomId);

        builder.Property(d => d.DisplayName).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Platform).HasMaxLength(32).IsRequired();
        builder.Property(d => d.LastSeenAt).HasColumnType("datetimeoffset");
    }
}
