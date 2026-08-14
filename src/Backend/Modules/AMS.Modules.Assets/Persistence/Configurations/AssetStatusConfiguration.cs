using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetStatus]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetStatusConfiguration : IEntityTypeConfiguration<AssetStatus>
{
    public void Configure(EntityTypeBuilder<AssetStatus> builder)
    {
        builder.ToTable("AssetStatus");

        builder.HasKey(x => x.Id).HasName("PK_AssetStatus");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsTerminal).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.StatusName)
            .IsUnique()
            .HasDatabaseName("UX_AssetStatus_Name");
    }
}
