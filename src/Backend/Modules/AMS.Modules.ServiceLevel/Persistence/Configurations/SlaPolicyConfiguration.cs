using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[SlaPolicy]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicy", table =>
        {
            table.HasCheckConstraint("CK_SlaPolicy_Priority", "([Priority] IN (N'Low', N'Medium', N'High', N'Critical'))");
            table.HasCheckConstraint("CK_SlaPolicy_Targets", "([ResponseTargetMinutes] > 0 AND [ResolutionTargetMinutes] > 0)");
            table.HasCheckConstraint("CK_SlaPolicy_ResponseWithinResolution", "([ResponseTargetMinutes] <= [ResolutionTargetMinutes])");
            table.HasCheckConstraint("CK_SlaPolicy_NearDue", "([NearDueWarningMinutes] >= 0)");
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("SysStartTime");
                temporal.HasPeriodEnd("SysEndTime");
                temporal.UseHistoryTable("SlaPolicyHistory", "ServiceLevel");
            });
        });

        builder.HasKey(x => x.Id).HasName("PK_SlaPolicy");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.PolicyName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Priority).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ResponseTargetMinutes).IsRequired();
        builder.Property(x => x.ResolutionTargetMinutes).IsRequired();
        builder.Property(x => x.RespectOperationalHours).IsRequired().HasDefaultValueSql("1", "DF_SlaPolicy_RespectOperationalHours").ValueGeneratedNever();
        builder.Property(x => x.RespectHolidays).IsRequired().HasDefaultValueSql("1", "DF_SlaPolicy_RespectHolidays").ValueGeneratedNever();
        builder.Property(x => x.RespectWeekends).IsRequired().HasDefaultValueSql("1", "DF_SlaPolicy_RespectWeekends").ValueGeneratedNever();
        builder.Property(x => x.NearDueWarningMinutes).IsRequired().HasDefaultValueSql("30", "DF_SlaPolicy_NearDueWarningMinutes").ValueGeneratedNever();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        // R2-22: the token for a system-versioned table. SysStartTime is history only.
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken().HasDefaultValueSql("NEWID()", "DF_SlaPolicy_ConcurrencyStamp");

        builder.HasIndex(x => x.Priority)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_SlaPolicy_ActivePriority");

        builder.HasIndex(x => x.PolicyName)
            .IsUnique()
            .HasDatabaseName("UX_SlaPolicy_Name");
    }
}
