using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetEvent]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetEventConfiguration : IEntityTypeConfiguration<AssetEvent>
{
    public void Configure(EntityTypeBuilder<AssetEvent> builder)
    {
        builder.ToTable("AssetEvent");

        builder.HasKey(x => x.Id).HasName("PK_AssetEvent");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.EventOnUtc).IsRequired();
        builder.Property(x => x.PerformedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EmployeeNameSnapshot).HasMaxLength(150);
        builder.Property(x => x.LocationNameSnapshot).HasMaxLength(100);
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 3);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetEvent_Asset_AssetId");

        builder.HasIndex(x => new { x.AssetId, x.EventOnUtc })
            .HasDatabaseName("IX_AssetEvent_Asset");
    }
}
