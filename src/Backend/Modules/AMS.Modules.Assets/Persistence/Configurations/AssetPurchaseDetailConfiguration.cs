using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetPurchaseDetail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetPurchaseDetailConfiguration : IEntityTypeConfiguration<AssetPurchaseDetail>
{
    public void Configure(EntityTypeBuilder<AssetPurchaseDetail> builder)
    {
        builder.ToTable("AssetPurchaseDetail", table =>
        {
            table.HasCheckConstraint("CK_AssetPurchaseDetail_WarrantyWindow", "([WarrantyEndDate] IS NULL OR [WarrantyStartDate] IS NULL OR [WarrantyEndDate] >= [WarrantyStartDate])");
        });

        builder.HasKey(x => x.AssetId).HasName("PK_AssetPurchaseDetail");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.PurchaseOrderNumber).HasMaxLength(50);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.PurchaseCost).HasPrecision(18, 2);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetPurchaseDetail_Asset_AssetId");
    }
}
