using AMS.Modules.SapSync.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.SapSync.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[SapSync].[SapSyncWatermark]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SapSyncWatermarkConfiguration : IEntityTypeConfiguration<SapSyncWatermark>
{
    public void Configure(EntityTypeBuilder<SapSyncWatermark> builder)
    {
        builder.ToTable("SapSyncWatermark");

        builder.HasKey(x => x.Id).HasName("PK_SapSyncWatermark");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.SyncType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastChangedOnUtc).IsRequired();
        builder.Property(x => x.UpdatedOnUtc).IsRequired();

        builder.HasIndex(x => x.SyncType)
            .IsUnique()
            .HasDatabaseName("UX_SapSyncWatermark_Type");
    }
}
