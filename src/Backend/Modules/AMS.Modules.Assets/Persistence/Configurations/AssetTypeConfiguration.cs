using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetType]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.ToTable("AssetType");

        builder.HasKey(x => x.Id).HasName("PK_AssetType");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.TypeName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsAllocatable).IsRequired().HasDefaultValueSql("1", "DF_AssetType_IsAllocatable").ValueGeneratedNever();
        builder.Property(x => x.IsPhysical).IsRequired().HasDefaultValueSql("1", "DF_AssetType_IsPhysical").ValueGeneratedNever();
        builder.Property(x => x.IsBulkDefault).IsRequired().HasDefaultValueSql("0", "DF_AssetType_IsBulkDefault").ValueGeneratedNever();
        builder.Property(x => x.TracksHardware).IsRequired().HasDefaultValueSql("0", "DF_AssetType_TracksHardware").ValueGeneratedNever();
        builder.Property(x => x.TracksSoftware).IsRequired().HasDefaultValueSql("0", "DF_AssetType_TracksSoftware").ValueGeneratedNever();
        builder.Property(x => x.TracksVehicle).IsRequired().HasDefaultValueSql("0", "DF_AssetType_TracksVehicle").ValueGeneratedNever();
        builder.Property(x => x.TracksCalibration).IsRequired().HasDefaultValueSql("0", "DF_AssetType_TracksCalibration").ValueGeneratedNever();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<AssetType>()
            .WithMany()
            .HasForeignKey(x => x.ParentAssetTypeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetType_AssetType_ParentAssetTypeId");

        builder.HasIndex(x => x.TypeName)
            .IsUnique()
            .HasDatabaseName("UX_AssetType_Name");

        builder.HasIndex(x => x.ParentAssetTypeId)
            .HasDatabaseName("IX_AssetType_ParentAssetTypeId");
    }
}
