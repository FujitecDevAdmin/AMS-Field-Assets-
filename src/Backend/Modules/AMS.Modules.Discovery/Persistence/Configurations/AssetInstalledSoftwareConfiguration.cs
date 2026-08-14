using AMS.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Discovery.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Discovery].[AssetInstalledSoftware]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetInstalledSoftwareConfiguration : IEntityTypeConfiguration<AssetInstalledSoftware>
{
    public void Configure(EntityTypeBuilder<AssetInstalledSoftware> builder)
    {
        builder.ToTable("AssetInstalledSoftware");

        builder.HasKey(x => x.Id).HasName("PK_AssetInstalledSoftware");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.SoftwareName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Version).HasMaxLength(80);
        builder.Property(x => x.Publisher).HasMaxLength(200);
        builder.Property(x => x.FirstSeenOnUtc).IsRequired();
        builder.Property(x => x.LastSeenOnUtc).IsRequired();
        builder.Property(x => x.IsRemoved).IsRequired();

        builder.HasIndex(x => x.SoftwareName)
            .HasDatabaseName("IX_AssetInstalledSoftware_Name");

        builder.HasIndex(x => new { x.AssetId, x.SoftwareName, x.Version })
            .IsUnique()
            .HasFilter("[Version] IS NOT NULL")
            .HasDatabaseName("UX_AssetInstalledSoftware_Install");
    }
}
