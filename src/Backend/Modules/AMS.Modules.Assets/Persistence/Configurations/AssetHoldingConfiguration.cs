using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetHolding]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetHoldingConfiguration : IEntityTypeConfiguration<AssetHolding>
{
    public void Configure(EntityTypeBuilder<AssetHolding> builder)
    {
        builder.ToTable("AssetHolding", table =>
        {
            table.HasCheckConstraint("CK_AssetHolding_NonNegative", "([OnHandQuantity] >= 0)");
            table.HasCheckConstraint("CK_AssetHolding_OnePlaceKind", "(([LocationId] IS NOT NULL AND [CustomerSiteId] IS NULL) OR ([LocationId] IS NULL AND [CustomerSiteId] IS NOT NULL))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetHolding");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.OnHandQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetHolding_Asset_AssetId");

        builder.HasIndex(x => new { x.AssetId, x.LocationId })
            .IsUnique()
            .HasFilter("[LocationId] IS NOT NULL")
            .HasDatabaseName("UX_AssetHolding_AssetLocation");

        builder.HasIndex(x => new { x.AssetId, x.CustomerSiteId })
            .IsUnique()
            .HasFilter("[CustomerSiteId] IS NOT NULL")
            .HasDatabaseName("UX_AssetHolding_AssetSite");

        builder.HasIndex(x => x.LocationId)
            .HasFilter("[OnHandQuantity] > 0")
            .HasDatabaseName("IX_AssetHolding_Location");
    }
}
