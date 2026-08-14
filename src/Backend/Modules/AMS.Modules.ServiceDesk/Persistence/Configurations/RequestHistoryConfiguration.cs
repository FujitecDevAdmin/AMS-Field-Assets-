using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestHistory]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestHistoryConfiguration : IEntityTypeConfiguration<RequestHistory>
{
    public void Configure(EntityTypeBuilder<RequestHistory> builder)
    {
        builder.ToTable("RequestHistory", table =>
        {
            table.HasCheckConstraint("CK_RequestHistory_EntryKind", "([EntryKind] IN (N'Transition', N'Note', N'Email', N'Automation', N'Sla', N'Escalation'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestHistory");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.EntryKind).HasMaxLength(20).IsRequired().HasDefaultValueSql("N'Transition'", "DF_RequestHistory_EntryKind").ValueGeneratedNever();
        builder.Property(x => x.EntryText).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)");
        builder.Property(x => x.IsInternal).IsRequired().HasDefaultValueSql("0", "DF_RequestHistory_IsInternal").ValueGeneratedNever();
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.PerformedBy).HasMaxLength(100).IsRequired();

        builder.HasOne<ServiceRequest>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestHistory_ServiceRequest_ServiceRequestId");

        builder.HasOne<RequestEmail>()
            .WithMany()
            .HasForeignKey(x => x.RequestEmailId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestHistory_RequestEmail_RequestEmailId");

        builder.HasIndex(x => new { x.ServiceRequestId, x.OccurredOnUtc })
            .HasDatabaseName("IX_RequestHistory_Request");

        builder.HasIndex(x => x.RequestEmailId)
            .HasDatabaseName("IX_RequestHistory_RequestEmailId");
    }
}
