using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalStageApproverRule]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ApprovalStageApproverRuleConfiguration : IEntityTypeConfiguration<ApprovalStageApproverRule>
{
    public void Configure(EntityTypeBuilder<ApprovalStageApproverRule> builder)
    {
        builder.ToTable("ApprovalStageApproverRule", table =>
        {
            table.HasCheckConstraint("CK_ApprovalStageApproverRule_ResolverType", "([ResolverType] IN ( N'User', N'Role', N'Capability', N'EmployeeManager', N'RequesterManager', N'LocationBranchAdmin', N'CustomEmail' ))");
            table.HasCheckConstraint("CK_ApprovalStageApproverRule_Value", "( ([ResolverType] = N'User' AND [ResolverUserId] IS NOT NULL) OR ([ResolverType] = N'Role' AND [ResolverRoleId] IS NOT NULL) OR ([ResolverType] = N'Capability' AND [ResolverCapabilityName] IS NOT NULL) OR ([ResolverType] = N'EmployeeManager') OR ([ResolverType] = N'RequesterManager') OR ([ResolverType] = N'LocationBranchAdmin' AND [ResolverCapabilityName] IS NOT NULL) OR ([ResolverType] = N'CustomEmail' AND [ResolverEmail] IS NOT NULL) )");
        });

        builder.HasKey(x => x.Id).HasName("PK_ApprovalStageApproverRule");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ApprovalWorkflowStageId).IsRequired();
        builder.Property(x => x.ResolverType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResolverCapabilityName).HasMaxLength(80);
        builder.Property(x => x.ResolverEmail).HasMaxLength(256);
        builder.Property(x => x.DisplayName).HasMaxLength(150);
        builder.Property(x => x.IsRequired).IsRequired().HasDefaultValueSql("1", "DF_ApprovalStageApproverRule_IsRequired").ValueGeneratedNever();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<ApprovalWorkflowStage>()
            .WithMany()
            .HasForeignKey(x => x.ApprovalWorkflowStageId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ApprovalStageApproverRule_ApprovalWorkflowStage_ApprovalWorkflowStageId");

        builder.HasIndex(x => new { x.ApprovalWorkflowStageId, x.IsEnabled })
            .HasDatabaseName("IX_ApprovalStageApproverRule_Stage");
    }
}
