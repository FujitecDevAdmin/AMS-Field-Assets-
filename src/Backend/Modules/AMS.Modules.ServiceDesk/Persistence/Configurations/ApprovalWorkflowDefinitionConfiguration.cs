using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalWorkflowDefinition]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ApprovalWorkflowDefinitionConfiguration : IEntityTypeConfiguration<ApprovalWorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowDefinition> builder)
    {
        builder.ToTable("ApprovalWorkflowDefinition", table =>
        {
            table.HasCheckConstraint("CK_ApprovalWorkflowDefinition_Version", "([VersionNumber] > 0)");
            table.HasCheckConstraint("CK_ApprovalWorkflowDefinition_Priority", "([Priority] IS NULL OR [Priority] IN (N'Low', N'Medium', N'High', N'Critical'))");
            table.HasCheckConstraint("CK_ApprovalWorkflowDefinition_EffectiveRange", "([EffectiveToUtc] IS NULL OR [EffectiveFromUtc] IS NULL OR [EffectiveToUtc] > [EffectiveFromUtc])");
        });

        builder.HasKey(x => x.Id).HasName("PK_ApprovalWorkflowDefinition");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.WorkflowName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Priority).HasMaxLength(20);
        builder.Property(x => x.IsDefault).IsRequired().HasDefaultValueSql("0", "DF_ApprovalWorkflowDefinition_IsDefault").ValueGeneratedNever();
        builder.Property(x => x.IsPublished).IsRequired().HasDefaultValueSql("0", "DF_ApprovalWorkflowDefinition_IsPublished").ValueGeneratedNever();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<ServiceTemplate>()
            .WithMany()
            .HasForeignKey(x => x.ServiceTemplateId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_ApprovalWorkflowDefinition_ServiceTemplate_ServiceTemplateId");

        builder.HasIndex(x => new { x.WorkflowName, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_ApprovalWorkflowDefinition_NameVersion");

        builder.HasIndex(x => x.IsDefault)
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsActive] = 1")
            .HasDatabaseName("UX_ApprovalWorkflowDefinition_OneActiveDefault");

        builder.HasIndex(x => new { x.ServiceTemplateId, x.LocationId, x.Priority, x.EffectiveFromUtc })
            .HasFilter("[IsActive] = 1 AND [IsPublished] = 1")
            .HasDatabaseName("IX_ApprovalWorkflowDefinition_Match");
    }
}
