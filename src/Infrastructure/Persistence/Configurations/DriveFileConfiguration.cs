using Lumora.Server.Domain.Drive;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumora.Server.Infrastructure.Persistence.Configurations;

public sealed class DriveFileConfiguration : IEntityTypeConfiguration<DriveFile>
{
    public void Configure(EntityTypeBuilder<DriveFile> builder)
    {
        builder.ToTable("DriveFiles");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.RoomId).IsRequired();
        builder.HasIndex(f => f.RoomId);

        builder.Property(f => f.EncryptedMetadata)
            .HasConversion(
                metadata => metadata.Bytes,
                bytes => EncryptedPayload.FromTrusted(bytes))
            .HasColumnType("varbinary(max)")
            .IsRequired();

        builder.Property(f => f.BlobId)
            .HasConversion(blobId => blobId.Value, value => BlobId.From(value))
            .IsRequired();

        builder.Property(f => f.SizeBytes).IsRequired();
        builder.Property(f => f.DeviceId).IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnType("datetimeoffset");
    }
}
