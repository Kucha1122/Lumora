using Lumora.Server.Domain.Updates;
using Lumora.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumora.Server.Infrastructure.Persistence.Configurations;

public sealed class UpdateReleaseConfiguration : IEntityTypeConfiguration<UpdateRelease>
{
    public void Configure(EntityTypeBuilder<UpdateRelease> builder)
    {
        builder.ToTable("UpdateReleases");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Version).HasMaxLength(32).IsRequired();

        builder.Property(r => r.VersionCode).IsRequired();
        builder.HasIndex(r => r.VersionCode).IsUnique();

        builder.Property(r => r.BlobId)
            .HasConversion(id => id.Value, value => BlobId.From(value))
            .IsRequired();

        builder.Property(r => r.SizeBytes).IsRequired();

        builder.Property(r => r.ReleaseNotes).HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).HasColumnType("datetimeoffset");
    }
}
