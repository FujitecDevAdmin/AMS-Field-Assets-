using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetHardwareDetail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetHardwareDetailConfiguration : IEntityTypeConfiguration<AssetHardwareDetail>
{
    public void Configure(EntityTypeBuilder<AssetHardwareDetail> builder)
    {
        builder.ToTable("AssetHardwareDetail");

        builder.HasKey(x => x.AssetId).HasName("PK_AssetHardwareDetail");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.Hostname).HasMaxLength(100);
        builder.Property(x => x.ChassisType).HasMaxLength(50);
        builder.Property(x => x.Processor).HasMaxLength(150);
        builder.Property(x => x.MonitorModel).HasMaxLength(100);
        builder.Property(x => x.MonitorSerialNumber).HasMaxLength(100);
        builder.Property(x => x.MacAddress).HasMaxLength(50);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetHardwareDetail_Asset_AssetId");
    }
}
