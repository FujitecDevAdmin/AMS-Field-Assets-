using AMS.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Discovery.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Discovery].[AssetHealthHistory]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetHealthHistoryConfiguration : IEntityTypeConfiguration<AssetHealthHistory>
{
    public void Configure(EntityTypeBuilder<AssetHealthHistory> builder)
    {
        builder.ToTable("AssetHealthHistory");

        builder.HasKey(x => x.Id).HasName("PK_AssetHealthHistory");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.CpuPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.MemoryPercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.SystemDrivePercent).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.CapturedOnUtc).IsRequired();

        builder.HasIndex(x => new { x.AssetId, x.CapturedOnUtc })
            .HasDatabaseName("IX_AssetHealthHistory_AssetTrend");

        builder.HasIndex(x => x.CapturedOnUtc)
            .HasDatabaseName("IX_AssetHealthHistory_Captured");
    }
}
