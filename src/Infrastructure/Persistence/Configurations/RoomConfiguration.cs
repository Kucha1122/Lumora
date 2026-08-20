using Lumora.Server.Domain.Rooms;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumora.Server.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Slug)
            .HasConversion(slug => slug.Value, value => RoomSlug.FromTrusted(value))
            .HasMaxLength(RoomSlug.MaxLength)
            .HasColumnType($"nvarchar({RoomSlug.MaxLength})")
            .IsRequired();

        builder.HasIndex(r => r.Slug).IsUnique();

        builder.Property(r => r.DisplayName).HasMaxLength(128).IsRequired();

        builder.Property(r => r.Visibility).HasConversion<int>();

        builder.Property(r => r.KdfSalt).HasColumnType("varbinary(64)");

        builder.Property(r => r.AuthKeyHash).HasColumnType("varbinary(64)");

        builder.Property(r => r.CreatedAt).HasColumnType("datetimeoffset");
    }
}
