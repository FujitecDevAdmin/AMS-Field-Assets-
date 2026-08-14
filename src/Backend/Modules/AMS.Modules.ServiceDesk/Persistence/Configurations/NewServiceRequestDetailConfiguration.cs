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
        builder.Property(x => x.NeedsEmail).IsRequired();
        builder.Property(x => x.NeedsErp).IsRequired();
        builder.Property(x => x.NeedsDms).IsRequired();
        builder.Property(x => x.NeedsVpn).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_NewServiceRequestDetail_ServiceRequest_ServiceRequestId");
    }
}
