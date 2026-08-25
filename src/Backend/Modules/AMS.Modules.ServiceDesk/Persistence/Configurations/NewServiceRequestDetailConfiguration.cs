using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[NewServiceRequestDetail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class NewServiceRequestDetailConfiguration : IEntityTypeConfiguration<NewServiceRequestDetail>
{
    public void Configure(EntityTypeBuilder<NewServiceRequestDetail> builder)
    {
        builder.ToTable("NewServiceRequestDetail");

        builder.HasKey(x => x.ServiceRequestId).HasName("PK_NewServiceRequestDetail");
        builder.Property(x => x.ServiceRequestId).ValueGeneratedNever();

        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.RequestCategoryId).IsRequired();
        builder.Property(x => x.RequestSubCategoryId).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_NewServiceRequestDetail_ServiceRequest_ServiceRequestId");

        builder.HasOne<RequestCategory>()
            .WithMany()
            .HasForeignKey(x => x.RequestCategoryId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_NewServiceRequestDetail_RequestCategory_RequestCategoryId");

        builder.HasOne<RequestSubCategory>()
            .WithMany()
            .HasForeignKey(x => new { x.RequestSubCategoryId, x.RequestCategoryId })
            .HasPrincipalKey(x => new { x.Id, x.RequestCategoryId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_NewServiceRequestDetail_RequestSubCategory_Category");
    }
}
