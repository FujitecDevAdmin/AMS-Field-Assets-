using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalStep]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestApprovalStepConfiguration : IEntityTypeConfiguration<RequestApprovalStep>
{
    public void Configure(EntityTypeBuilder<RequestApprovalStep> builder)
    {
        builder.ToTable("RequestApprovalStep", table =>
        {
            table.HasCheckConstraint("CK_RequestApprovalStep_Number", "([StageNumber] > 0)");
            table.HasCheckConstraint("CK_RequestApprovalStep_Mode", "([ApprovalModeSnapshot] IN (N'Any', N'All'))");
            table.HasCheckConstraint("CK_RequestApprovalStep_Status", "([Status] IN (N'Waiting', N'Pending', N'Approved', N'Rejected', N'Skipped', N'Cancelled'))");
            table.HasCheckConstraint("CK_RequestApprovalStep_Activation", "([Status] IN (N'Waiting', N'Cancelled', N'Skipped') OR [ActivatedOnUtc] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestApprovalStep");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RequestApprovalInstanceId).IsRequired();
        builder.Property(x => x.ApprovalWorkflowStageId).IsRequired();
        builder.Property(x => x.StageNumber).IsRequired();
        builder.Property(x => x.StageNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ApprovalModeSnapshot).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.OutcomeRemarks).HasMaxLength(1000);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<RequestApprovalInstance>()
            .WithMany()
            .HasForeignKey(x => x.RequestApprovalInstanceId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestApprovalStep_RequestApprovalInstance_RequestApprovalInstanceId");

        builder.HasOne<ApprovalWorkflowStage>()
            .WithMany()
            .HasForeignKey(x => x.ApprovalWorkflowStageId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestApprovalStep_ApprovalWorkflowStage_ApprovalWorkflowStageId");

        builder.HasIndex(x => new { x.RequestApprovalInstanceId, x.StageNumber })
            .IsUnique()
            .HasDatabaseName("UX_RequestApprovalStep_Number");

        builder.HasIndex(x => x.RequestApprovalInstanceId)
            .IsUnique()
            .HasFilter("[Status] = N'Pending'")
            .HasDatabaseName("UX_RequestApprovalStep_OnePending");

        builder.HasIndex(x => x.DueOnUtc)
            .HasFilter("[Status] = N'Pending'")
            .HasDatabaseName("IX_RequestApprovalStep_Due");
    }
}
