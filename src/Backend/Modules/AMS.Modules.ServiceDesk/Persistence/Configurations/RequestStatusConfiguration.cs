using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestStatus]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestStatusConfiguration : IEntityTypeConfiguration<RequestStatus>
{
    public void Configure(EntityTypeBuilder<RequestStatus> builder)
    {
        builder.ToTable("RequestStatus", table =>
        {
            table.HasCheckConstraint("CK_RequestStatus_SlaClockBehaviour", "([SlaClockBehaviour] IN (N'Running', N'Paused', N'Stopped'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestStatus");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.StatusName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsClosedState).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.SlaClockBehaviour).HasMaxLength(10).IsRequired().HasDefaultValueSql("N'Running'", "DF_RequestStatus_SlaClockBehaviour").ValueGeneratedNever();
        builder.Property(x => x.CountsTechnicianTime).IsRequired().HasDefaultValueSql("0", "DF_RequestStatus_CountsTechnicianTime").ValueGeneratedNever();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.StatusName)
            .IsUnique()
            .HasDatabaseName("UX_RequestStatus_Name");
    }
}
