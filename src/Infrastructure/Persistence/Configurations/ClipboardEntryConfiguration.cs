using Lumora.Server.Domain.Clipboard;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumora.Server.Infrastructure.Persistence.Configurations;

public sealed class ClipboardEntryConfiguration : IEntityTypeConfiguration<ClipboardEntry>
{
    public void Configure(EntityTypeBuilder<ClipboardEntry> builder)
    {
        builder.ToTable("ClipboardEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RoomId).IsRequired();
        builder.HasIndex(e => new { e.RoomId, e.CreatedAt });

        builder.Property(e => e.Kind).HasConversion<int>();

        builder.Property(e => e.InlinePayload)
            .HasConversion(
                payload => payload == null ? null : payload.Bytes,
                bytes => bytes == null ? null : EncryptedPayload.FromTrusted(bytes))
            .HasColumnType("varbinary(max)");

        builder.Property(e => e.BlobId)
            .HasConversion(
                blobId => blobId == null ? (Guid?)null : blobId.Value,
                value => value == null ? null : BlobId.From(value.Value));

        builder.Property(e => e.SizeBytes).IsRequired();
        builder.Property(e => e.DeviceId).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnType("datetimeoffset");
    }
}
