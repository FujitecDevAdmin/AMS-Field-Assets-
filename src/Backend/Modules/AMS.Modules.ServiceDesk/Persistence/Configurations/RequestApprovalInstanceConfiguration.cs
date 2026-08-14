using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalInstance]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestApprovalInstanceConfiguration : IEntityTypeConfiguration<RequestApprovalInstance>
{
    public void Configure(EntityTypeBuilder<RequestApprovalInstance> builder)
    {
        builder.ToTable("RequestApprovalInstance", table =>
        {
            table.HasCheckConstraint("CK_RequestApprovalInstance_Status", "([Status] IN (N'Pending', N'Approved', N'Rejected', N'Cancelled'))");
            table.HasCheckConstraint("CK_RequestApprovalInstance_Version", "([WorkflowVersion] > 0)");
            table.HasCheckConstraint("CK_RequestApprovalInstance_CurrentStage", "([CurrentStageNumber] IS NULL OR [CurrentStageNumber] > 0)");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestApprovalInstance");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.ApprovalWorkflowId).IsRequired();
        builder.Property(x => x.WorkflowNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(x => x.WorkflowVersion).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SubmittedByUserId).IsRequired();
        builder.Property(x => x.SubmittedOnUtc).IsRequired();
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestApprovalInstance_ServiceRequest_ServiceRequestId");

        builder.HasOne<ApprovalWorkflowDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestApprovalInstance_ApprovalWorkflowDefinition_ApprovalWorkflowId");

        builder.HasIndex(x => x.ServiceRequestId)
            .IsUnique()
            .HasFilter("[Status] = N'Pending'")
            .HasDatabaseName("UX_RequestApprovalInstance_OnePending");

        builder.HasIndex(x => new { x.ServiceRequestId, x.SubmittedOnUtc })
            .HasDatabaseName("IX_RequestApprovalInstance_Request");
    }
}
