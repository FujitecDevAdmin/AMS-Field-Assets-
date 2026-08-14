using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetClass]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetClassConfiguration : IEntityTypeConfiguration<AssetClass>
{
    public void Configure(EntityTypeBuilder<AssetClass> builder)
    {
        builder.ToTable("AssetClass");

        builder.HasKey(x => x.Id).HasName("PK_AssetClass");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ClassCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ClassName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReportingCategory).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDepreciable).IsRequired();
        builder.Property(x => x.IsIntangible).IsRequired();
        builder.Property(x => x.IsAuc).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.IsAuc)
            .IsUnique()
            .HasFilter("[IsAuc] = 1")
            .HasDatabaseName("UX_AssetClass_OneAuc");

        builder.HasIndex(x => x.ClassCode)
            .IsUnique()
            .HasDatabaseName("UX_AssetClass_Code");

        builder.HasIndex(x => x.ClassName)
            .IsUnique()
            .HasDatabaseName("UX_AssetClass_Name");
    }
}
