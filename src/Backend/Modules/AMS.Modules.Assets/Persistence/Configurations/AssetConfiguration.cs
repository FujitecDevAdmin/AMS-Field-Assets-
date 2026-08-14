using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[Asset]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Asset", table =>
        {
            table.HasCheckConstraint("CK_Asset_QuantityPositive", "([Quantity] > 0)");
            table.HasCheckConstraint("CK_Asset_UnitQuantityIsOne", "([IsBulk] = 1 OR [Quantity] = 1)");
            table.HasCheckConstraint("CK_Asset_BulkHasUom", "([IsBulk] = 0 OR [UnitOfMeasure] IS NOT NULL)");
            table.HasCheckConstraint("CK_Asset_BulkNotHeld", "([IsBulk] = 0 OR ([CurrentEmployeeId] IS NULL AND [CurrentLocationId] IS NULL))");
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("SysStartTime");
                temporal.HasPeriodEnd("SysEndTime");
                temporal.UseHistoryTable("AssetHistory", "Assets");
            });
        });

        builder.HasKey(x => x.Id).HasName("PK_Asset");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AssetName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.AssetTypeId).IsRequired();
        builder.Property(x => x.Make).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.AssetStatusId).IsRequired();
        builder.Property(x => x.CostCenter).HasMaxLength(40);
        builder.Property(x => x.QrCodeValue).HasMaxLength(100);
        builder.Property(x => x.BarcodeValue).HasMaxLength(100);
        builder.Property(x => x.ErpAssetNumber).HasMaxLength(50);
        builder.Property(x => x.SapAssetNumber).HasMaxLength(50);
        builder.Property(x => x.SapAssetClass).HasMaxLength(50);
        builder.Property(x => x.SapPlant).HasMaxLength(20);
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.ImportedDataJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsBulk).IsRequired().HasDefaultValueSql("0", "DF_Asset_IsBulk").ValueGeneratedNever();
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired().HasDefaultValueSql("1", "DF_Asset_Quantity").ValueGeneratedNever();
        builder.Property(x => x.UnitOfMeasure).HasMaxLength(20);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        // R2-22: the token for a system-versioned table. SysStartTime is history only.
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken().HasDefaultValueSql("NEWID()", "DF_Asset_ConcurrencyStamp");

        builder.HasOne<AssetType>()
            .WithMany()
            .HasForeignKey(x => x.AssetTypeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Asset_AssetType_AssetTypeId");

        builder.HasOne<AssetClass>()
            .WithMany()
            .HasForeignKey(x => x.AssetClassId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Asset_AssetClass_AssetClassId");

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.CapitalisedFromAssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Asset_Asset_CapitalisedFromAssetId");

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.SplitFromAssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Asset_Asset_SplitFromAssetId");

        builder.HasOne<AssetStatus>()
            .WithMany()
            .HasForeignKey(x => x.AssetStatusId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Asset_AssetStatus_AssetStatusId");

        builder.HasIndex(x => x.AssetNumber)
            .IsUnique()
            .HasDatabaseName("UX_Asset_Number");

        builder.HasIndex(x => x.QrCodeValue)
            .IsUnique()
            .HasFilter("[QrCodeValue] IS NOT NULL")
            .HasDatabaseName("UX_Asset_QrCode");

        builder.HasIndex(x => x.SapAssetNumber)
            .IsUnique()
            .HasFilter("[SapAssetNumber] IS NOT NULL")
            .HasDatabaseName("UX_Asset_SapNumber");

        builder.HasIndex(x => new { x.CurrentLocationId, x.AssetStatusId })
            .HasDatabaseName("IX_Asset_LocationStatus");

        builder.HasIndex(x => x.SerialNumber)
            .HasDatabaseName("IX_Asset_Serial");

        builder.HasIndex(x => x.AssetTypeId)
            .HasDatabaseName("IX_Asset_AssetTypeId");

        builder.HasIndex(x => x.AssetStatusId)
            .HasDatabaseName("IX_Asset_AssetStatusId");

        builder.HasIndex(x => x.ImportBatchId)
            .HasFilter("[ImportBatchId] IS NOT NULL")
            .HasDatabaseName("IX_Asset_ImportBatchId");

        builder.HasIndex(x => x.AssetClassId)
            .HasDatabaseName("IX_Asset_AssetClassId");

        builder.HasIndex(x => x.CapitalisedFromAssetId)
            .HasDatabaseName("IX_Asset_CapitalisedFromAssetId");
    }
}
