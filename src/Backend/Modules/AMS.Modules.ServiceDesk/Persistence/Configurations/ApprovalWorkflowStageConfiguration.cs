using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalWorkflowStage]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ApprovalWorkflowStageConfiguration : IEntityTypeConfiguration<ApprovalWorkflowStage>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowStage> builder)
    {
        builder.ToTable("ApprovalWorkflowStage", table =>
        {
            table.HasCheckConstraint("CK_ApprovalWorkflowStage_Number", "([StageNumber] > 0)");
            table.HasCheckConstraint("CK_ApprovalWorkflowStage_Mode", "([ApprovalMode] IN (N'Any', N'All'))");
            table.HasCheckConstraint("CK_ApprovalWorkflowStage_Timers", "( ([DueAfterMinutes] IS NULL OR [DueAfterMinutes] > 0) AND ([ReminderAfterMinutes] IS NULL OR [ReminderAfterMinutes] > 0) AND ([ReminderRepeatMinutes] IS NULL OR [ReminderRepeatMinutes] > 0) AND ([EscalateAfterMinutes] IS NULL OR [EscalateAfterMinutes] >= 0) )");
        });

        builder.HasKey(x => x.Id).HasName("PK_ApprovalWorkflowStage");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ApprovalWorkflowId).IsRequired();
        builder.Property(x => x.StageNumber).IsRequired();
        builder.Property(x => x.StageName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ApprovalMode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.AllowDelegation).IsRequired().HasDefaultValueSql("0", "DF_ApprovalWorkflowStage_AllowDelegation").ValueGeneratedNever();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<ApprovalWorkflowDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ApprovalWorkflowStage_ApprovalWorkflowDefinition_ApprovalWorkflowId");

        builder.HasIndex(x => new { x.ApprovalWorkflowId, x.StageNumber })
            .IsUnique()
            .HasDatabaseName("UX_ApprovalWorkflowStage_Number");
    }
}
