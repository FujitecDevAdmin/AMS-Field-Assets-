using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestSubCategory]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestSubCategoryConfiguration : IEntityTypeConfiguration<RequestSubCategory>
{
    public void Configure(EntityTypeBuilder<RequestSubCategory> builder)
    {
        builder.ToTable("RequestSubCategory");

        builder.HasKey(x => x.Id).HasName("PK_RequestSubCategory");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RequestCategoryId).IsRequired();
        builder.Property(x => x.SubCategoryName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<RequestCategory>()
            .WithMany()
            .HasForeignKey(x => x.RequestCategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestSubCategory_RequestCategory_RequestCategoryId");

        builder.HasIndex(x => new { x.RequestCategoryId, x.SubCategoryName })
            .IsUnique()
            .HasDatabaseName("UX_RequestSubCategory_Name");
    }
}
