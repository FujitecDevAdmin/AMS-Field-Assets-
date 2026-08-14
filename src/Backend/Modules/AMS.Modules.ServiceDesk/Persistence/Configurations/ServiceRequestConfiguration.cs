using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ServiceRequest]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequest", table =>
        {
            table.HasCheckConstraint("CK_ServiceRequest_Kind", "([RequestKind] IN (N'SupportTicket', N'AssetIssue', N'NewService'))");
            table.HasCheckConstraint("CK_ServiceRequest_Priority", "([Priority] IN (N'Low', N'Medium', N'High', N'Critical'))");
            table.HasCheckConstraint("CK_ServiceRequest_SlaMinutes", "([ResolutionConsumedMinutes] >= 0 AND [TechnicianWorkingMinutes] >= 0 AND [SlaPausedMinutes] >= 0 AND ([ResponseElapsedMinutes] IS NULL OR [ResponseElapsedMinutes] >= 0))");
            table.HasCheckConstraint("CK_ServiceRequest_ScheduledHold", "([IsScheduledHold] = 0 OR [NextOperationalStartUtc] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_ServiceRequest");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RequestNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RequestKind).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Priority).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RequestStatusId).IsRequired();
        builder.Property(x => x.ManualAssetText).HasMaxLength(200);
        builder.Property(x => x.RequestedByEmployeeId).IsRequired();
        builder.Property(x => x.Resolution).HasMaxLength(4000);
        builder.Property(x => x.IsScheduledHold).IsRequired().HasDefaultValueSql("0", "DF_ServiceRequest_IsScheduledHold").ValueGeneratedNever();
        builder.Property(x => x.ScheduleHoldReason).HasMaxLength(300);
        builder.Property(x => x.ResolutionConsumedMinutes).IsRequired().HasDefaultValueSql("0", "DF_ServiceRequest_ResolutionConsumedMinutes").ValueGeneratedNever();
        builder.Property(x => x.TechnicianWorkingMinutes).IsRequired().HasDefaultValueSql("0", "DF_ServiceRequest_TechnicianWorkingMinutes").ValueGeneratedNever();
        builder.Property(x => x.SlaPausedMinutes).IsRequired().HasDefaultValueSql("0", "DF_ServiceRequest_SlaPausedMinutes").ValueGeneratedNever();
        builder.Property(x => x.IsSlaPaused).IsRequired().HasDefaultValueSql("0", "DF_ServiceRequest_IsSlaPaused").ValueGeneratedNever();
        builder.Property(x => x.IsSlaOverdue).IsRequired().HasDefaultValueSql("0", "DF_ServiceRequest_IsSlaOverdue").ValueGeneratedNever();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<RequestStatus>()
            .WithMany()
            .HasForeignKey(x => x.RequestStatusId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceRequest_RequestStatus_RequestStatusId");

        builder.HasOne<RequestCategory>()
            .WithMany()
            .HasForeignKey(x => x.RequestCategoryId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceRequest_RequestCategory_RequestCategoryId");

        builder.HasOne<RequestSubCategory>()
            .WithMany()
            .HasForeignKey(x => x.RequestSubCategoryId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceRequest_RequestSubCategory_RequestSubCategoryId");

        builder.HasOne<SupportTeam>()
            .WithMany()
            .HasForeignKey(x => x.AssignedTeamId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceRequest_SupportTeam_AssignedTeamId");

        builder.HasOne<ServiceTemplate>()
            .WithMany()
            .HasForeignKey(x => x.ServiceTemplateId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ServiceRequest_ServiceTemplate_ServiceTemplateId");

        builder.HasIndex(x => x.RequestNumber)
            .IsUnique()
            .HasDatabaseName("UX_ServiceRequest_Number");

        builder.HasIndex(x => new { x.RequestStatusId, x.LocationId, x.Priority })
            .HasDatabaseName("IX_ServiceRequest_Queue");

        builder.HasIndex(x => x.RequestedByEmployeeId)
            .HasDatabaseName("IX_ServiceRequest_Requester");

        builder.HasIndex(x => new { x.IsSlaOverdue, x.ResolutionDueOnUtc })
            .HasFilter("[ClosedOnUtc] IS NULL")
            .HasDatabaseName("IX_ServiceRequest_SlaQueue");

        builder.HasIndex(x => x.NextOperationalStartUtc)
            .HasFilter("[IsScheduledHold] = 1")
            .HasDatabaseName("IX_ServiceRequest_ScheduledIntake");

        builder.HasIndex(x => new { x.AssignedTeamId, x.RequestStatusId })
            .HasDatabaseName("IX_ServiceRequest_AssignedTeam");
    }
}
