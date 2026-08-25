using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestCategory]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestCategoryConfiguration : IEntityTypeConfiguration<RequestCategory>
{
    public void Configure(EntityTypeBuilder<RequestCategory> builder)
    {
        builder.ToTable("RequestCategory", table => table.HasCheckConstraint(
            "CK_RequestCategory_CategoryType",
            "([CategoryType] IN (N'Service', N'Incident'))"));

        builder.HasKey(x => x.Id).HasName("PK_RequestCategory");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CategoryType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.CategoryName)
            .IsUnique()
            .HasDatabaseName("UX_RequestCategory_Name");
    }
}
