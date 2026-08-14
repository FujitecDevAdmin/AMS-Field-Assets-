using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalNotificationLog]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ApprovalNotificationLogConfiguration : IEntityTypeConfiguration<ApprovalNotificationLog>
{
    public void Configure(EntityTypeBuilder<ApprovalNotificationLog> builder)
    {
        builder.ToTable("ApprovalNotificationLog", table =>
        {
            table.HasCheckConstraint("CK_ApprovalNotificationLog_Type", "([NotificationType] IN ( N'ApprovalRequired', N'Reminder', N'Escalation', N'StepApproved', N'RequestApproved', N'RequestRejected', N'RequestCancelled' ))");
            table.HasCheckConstraint("CK_ApprovalNotificationLog_Status", "([Status] IN (N'Queued', N'Sent', N'Failed', N'Skipped'))");
            table.HasCheckConstraint("CK_ApprovalNotificationLog_Attempts", "([AttemptCount] >= 0)");
        });

        builder.HasKey(x => x.Id).HasName("PK_ApprovalNotificationLog");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RequestApprovalInstanceId).IsRequired();
        builder.Property(x => x.NotificationType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.IdempotencyKey).IsRequired();
        builder.Property(x => x.RecipientAddress).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SubjectSnapshot).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AttemptCount).IsRequired().HasDefaultValueSql("0", "DF_ApprovalNotificationLog_AttemptCount").ValueGeneratedNever();
        builder.Property(x => x.LastError).HasMaxLength(500);
        builder.Property(x => x.QueuedOnUtc).IsRequired();

        builder.HasOne<RequestApprovalInstance>()
            .WithMany()
            .HasForeignKey(x => x.RequestApprovalInstanceId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ApprovalNotificationLog_RequestApprovalInstance_RequestApprovalInstanceId");

        builder.HasOne<RequestApprovalStep>()
            .WithMany()
            .HasForeignKey(x => x.RequestApprovalStepId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ApprovalNotificationLog_RequestApprovalStep_RequestApprovalStepId");

        builder.HasOne<RequestApprovalParticipant>()
            .WithMany()
            .HasForeignKey(x => x.RequestApprovalParticipantId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ApprovalNotificationLog_RequestApprovalParticipant_RequestApprovalParticipantId");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_ApprovalNotificationLog_Idempotency");

        builder.HasIndex(x => new { x.RequestApprovalInstanceId, x.QueuedOnUtc })
            .HasDatabaseName("IX_ApprovalNotificationLog_Instance");

        builder.HasIndex(x => x.EmailOutboxId)
            .HasFilter("[EmailOutboxId] IS NOT NULL")
            .HasDatabaseName("IX_ApprovalNotificationLog_Outbox");
    }
}
