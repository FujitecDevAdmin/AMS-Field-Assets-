using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalParticipant]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestApprovalParticipantConfiguration : IEntityTypeConfiguration<RequestApprovalParticipant>
{
    public void Configure(EntityTypeBuilder<RequestApprovalParticipant> builder)
    {
        builder.ToTable("RequestApprovalParticipant", table =>
        {
            table.HasCheckConstraint("CK_RequestApprovalParticipant_Identity", "([ApproverUserId] IS NOT NULL OR [ApproverEmployeeId] IS NOT NULL OR [ApproverEmailSnapshot] <> N'')");
            table.HasCheckConstraint("CK_RequestApprovalParticipant_Status", "([ParticipantStatus] IN (N'Waiting', N'Pending', N'Approved', N'Rejected', N'Delegated', N'Cancelled'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestApprovalParticipant");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RequestApprovalStepId).IsRequired();
        builder.Property(x => x.ApproverRuleId).IsRequired();
        builder.Property(x => x.ApproverNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ApproverEmailSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.ParticipantStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<RequestApprovalStep>()
            .WithMany()
            .HasForeignKey(x => x.RequestApprovalStepId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestApprovalParticipant_RequestApprovalStep_RequestApprovalStepId");

        builder.HasOne<ApprovalStageApproverRule>()
            .WithMany()
            .HasForeignKey(x => x.ApproverRuleId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestApprovalParticipant_ApprovalStageApproverRule_ApproverRuleId");

        builder.HasIndex(x => new { x.RequestApprovalStepId, x.ApproverRuleId, x.ApproverEmailSnapshot })
            .IsUnique()
            .HasDatabaseName("UX_RequestApprovalParticipant_Resolved");

        builder.HasIndex(x => new { x.ApproverUserId, x.ParticipantStatus, x.RequestApprovalStepId })
            .HasDatabaseName("IX_RequestApprovalParticipant_Inbox");
    }
}
