using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AssetSiteMapping]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetSiteMappingConfiguration : IEntityTypeConfiguration<AssetSiteMapping>
{
    public void Configure(EntityTypeBuilder<AssetSiteMapping> builder)
    {
        builder.ToTable("AssetSiteMapping");

        builder.HasKey(x => x.Id).HasName("PK_AssetSiteMapping");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.CustomerSiteId).IsRequired();
        builder.Property(x => x.MappedOnUtc).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<CustomerSite>()
            .WithMany()
            .HasForeignKey(x => x.CustomerSiteId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetSiteMapping_CustomerSite_CustomerSiteId");

        builder.HasIndex(x => x.AssetId)
            .IsUnique()
            .HasFilter("[RemovedOnUtc] IS NULL")
            .HasDatabaseName("UX_AssetSiteMapping_OneActivePerAsset");

        builder.HasIndex(x => x.CustomerSiteId)
            .HasDatabaseName("IX_AssetSiteMapping_CustomerSiteId");
    }
}
