using AMS.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Audit.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Audit].[ScheduledFieldChange]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ScheduledFieldChangeConfiguration : IEntityTypeConfiguration<ScheduledFieldChange>
{
    public void Configure(EntityTypeBuilder<ScheduledFieldChange> builder)
    {
        builder.ToTable("ScheduledFieldChange", table =>
        {
            table.HasCheckConstraint("CK_ScheduledFieldChange_Status", "([Status] IN (N'Pending', N'Applied', N'Cancelled', N'Failed', N'Superseded'))");
            table.HasCheckConstraint("CK_ScheduledFieldChange_Window", "([EffectiveToDate] IS NULL OR [EffectiveToDate] >= [EffectiveFromDate])");
            table.HasCheckConstraint("CK_ScheduledFieldChange_Applied", "([Status] <> N'Applied' OR [AppliedOnUtc] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_ScheduledFieldChange");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.SchemaName).HasMaxLength(60).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CurrentValue).HasMaxLength(1024);
        builder.Property(x => x.NewValue).HasMaxLength(1024);
        builder.Property(x => x.EffectiveFromDate).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequestedByUserId).IsRequired();
        builder.Property(x => x.RequestedOnUtc).IsRequired();
        builder.Property(x => x.AppliedBy).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.SchemaName, x.EntityName, x.EntityId, x.FieldName, x.EffectiveFromDate })
            .IsUnique()
            .HasFilter("[Status] = N'Pending'")
            .HasDatabaseName("UX_ScheduledFieldChange_OnePendingPerFieldPerDate");

        builder.HasIndex(x => x.EffectiveFromDate)
            .HasFilter("[Status] = N'Pending'")
            .HasDatabaseName("IX_ScheduledFieldChange_Due");

        builder.HasIndex(x => new { x.SchemaName, x.EntityName, x.EntityId, x.EffectiveFromDate })
            .HasDatabaseName("IX_ScheduledFieldChange_Entity");
    }
}
