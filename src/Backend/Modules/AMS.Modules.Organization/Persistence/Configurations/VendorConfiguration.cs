using AMS.Modules.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Organization.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Organization].[Vendor]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendor");

        builder.HasKey(x => x.Id).HasName("PK_Vendor");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.VendorName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(120);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.VendorName)
            .IsUnique()
            .HasDatabaseName("UX_Vendor_Name");
    }
}
