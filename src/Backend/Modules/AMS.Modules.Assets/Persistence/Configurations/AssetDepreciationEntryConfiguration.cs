using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetDepreciationEntry]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetDepreciationEntryConfiguration : IEntityTypeConfiguration<AssetDepreciationEntry>
{
    public void Configure(EntityTypeBuilder<AssetDepreciationEntry> builder)
    {
        builder.ToTable("AssetDepreciationEntry", table =>
        {
            table.HasCheckConstraint("CK_AssetDepreciationEntry_Source", "([SourceSystem] IN (N'Sap', N'Import'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetDepreciationEntry");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.FinancialYear).IsRequired();
        builder.Property(x => x.OpeningAccumulated).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Additions).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ChargedForPeriod).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ClosingAccumulated).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.NetBookValueAtClose).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.SourceSystem).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SyncedOnUtc).IsRequired();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetDepreciationEntry_Asset_AssetId");

        builder.HasIndex(x => new { x.AssetId, x.FinancialYear })
            .IsUnique()
            .HasDatabaseName("UX_AssetDepreciationEntry_AssetYear");
    }
}
