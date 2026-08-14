using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[CustomerSite]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class CustomerSiteConfiguration : IEntityTypeConfiguration<CustomerSite>
{
    public void Configure(EntityTypeBuilder<CustomerSite> builder)
    {
        builder.ToTable("CustomerSite");

        builder.HasKey(x => x.Id).HasName("PK_CustomerSite");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.SiteName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
    }
}
