using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[NewServiceRequestItem]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class NewServiceRequestItemConfiguration : IEntityTypeConfiguration<NewServiceRequestItem>
{
    public void Configure(EntityTypeBuilder<NewServiceRequestItem> builder)
    {
        builder.ToTable("NewServiceRequestItem", table =>
        {
            table.HasCheckConstraint("CK_NewServiceRequestItem_PositiveQuantity", "([Quantity] > 0)");
        });

        builder.HasKey(x => x.Id).HasName("PK_NewServiceRequestItem");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.AssetTypeId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.Specification).HasMaxLength(500);

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_NewServiceRequestItem_ServiceRequest_ServiceRequestId");

        builder.HasIndex(x => x.ServiceRequestId)
            .HasDatabaseName("IX_NewServiceRequestItem_ServiceRequestId");
    }
}
