using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetVehicleDetail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetVehicleDetailConfiguration : IEntityTypeConfiguration<AssetVehicleDetail>
{
    public void Configure(EntityTypeBuilder<AssetVehicleDetail> builder)
    {
        builder.ToTable("AssetVehicleDetail");

        builder.HasKey(x => x.AssetId).HasName("PK_AssetVehicleDetail");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.RegistrationNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ChassisNumber).HasMaxLength(50);
        builder.Property(x => x.EngineNumber).HasMaxLength(50);
        builder.Property(x => x.FuelType).HasMaxLength(20);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetVehicleDetail_Asset_AssetId");

        builder.HasIndex(x => x.RegistrationNumber)
            .IsUnique()
            .HasDatabaseName("UX_AssetVehicleDetail_Registration");
    }
}
