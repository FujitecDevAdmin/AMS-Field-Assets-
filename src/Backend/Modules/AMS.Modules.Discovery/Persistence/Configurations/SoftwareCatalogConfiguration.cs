using AMS.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Discovery.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Discovery].[SoftwareCatalog]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SoftwareCatalogConfiguration : IEntityTypeConfiguration<SoftwareCatalog>
{
    public void Configure(EntityTypeBuilder<SoftwareCatalog> builder)
    {
        builder.ToTable("SoftwareCatalog");

        builder.HasKey(x => x.Id).HasName("PK_SoftwareCatalog");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.SoftwareName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Publisher).HasMaxLength(200);
        builder.Property(x => x.IsBlacklisted).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.SoftwareName)
            .IsUnique()
            .HasDatabaseName("UX_SoftwareCatalog_Name");
    }
}
