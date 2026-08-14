using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetSoftwareDetail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetSoftwareDetailConfiguration : IEntityTypeConfiguration<AssetSoftwareDetail>
{
    public void Configure(EntityTypeBuilder<AssetSoftwareDetail> builder)
    {
        builder.ToTable("AssetSoftwareDetail");

        builder.HasKey(x => x.AssetId).HasName("PK_AssetSoftwareDetail");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.OperatingSystem).HasMaxLength(120);
        builder.Property(x => x.OperatingSystemBuild).HasMaxLength(60);
        builder.Property(x => x.Architecture).HasMaxLength(20);
        builder.Property(x => x.OfficeVersion).HasMaxLength(80);
        builder.Property(x => x.Antivirus).HasMaxLength(120);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetSoftwareDetail_Asset_AssetId");
    }
}
