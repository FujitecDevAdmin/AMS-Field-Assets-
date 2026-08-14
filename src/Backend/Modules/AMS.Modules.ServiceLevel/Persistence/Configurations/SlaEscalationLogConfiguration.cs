using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[SlaEscalationLog]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SlaEscalationLogConfiguration : IEntityTypeConfiguration<SlaEscalationLog>
{
    public void Configure(EntityTypeBuilder<SlaEscalationLog> builder)
    {
        builder.ToTable("SlaEscalationLog", table =>
        {
            table.HasCheckConstraint("CK_SlaEscalationLog_Outcome", "([Outcome] IN (N'Queued', N'Sent', N'Failed', N'Skipped'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_SlaEscalationLog");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.SlaEscalationId).IsRequired();
        builder.Property(x => x.EscalationType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Level).IsRequired();
        builder.Property(x => x.SentTo).HasMaxLength(400).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.FiredOnUtc).IsRequired();

        builder.HasOne<SlaEscalation>()
            .WithMany()
            .HasForeignKey(x => x.SlaEscalationId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_SlaEscalationLog_SlaEscalation_SlaEscalationId");

        builder.HasIndex(x => new { x.ServiceRequestId, x.SlaEscalationId })
            .IsUnique()
            .HasFilter("[Outcome] <> N'Failed'")
            .HasDatabaseName("UX_SlaEscalationLog_OncePerLevel");

        builder.HasIndex(x => new { x.ServiceRequestId, x.FiredOnUtc })
            .HasDatabaseName("IX_SlaEscalationLog_Request");
    }
}
