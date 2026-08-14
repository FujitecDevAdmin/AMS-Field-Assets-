using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestApprovalDecision]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class RequestApprovalDecisionConfiguration : IEntityTypeConfiguration<RequestApprovalDecision>
{
    public void Configure(EntityTypeBuilder<RequestApprovalDecision> builder)
    {
        builder.ToTable("RequestApprovalDecision", table =>
        {
            table.HasCheckConstraint("CK_RequestApprovalDecision_Decision", "([Decision] IN (N'Approved', N'Rejected'))");
            table.HasCheckConstraint("CK_RequestApprovalDecision_Source", "([Source] IN (N'Application', N'EmailLink', N'Api'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_RequestApprovalDecision");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RequestApprovalParticipantId).IsRequired();
        builder.Property(x => x.ClientDecisionId).IsRequired();
        builder.Property(x => x.Decision).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.ActedByEmailSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DecidedOnUtc).IsRequired();
        builder.Property(x => x.SourceIpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasOne<RequestApprovalParticipant>()
            .WithMany()
            .HasForeignKey(x => x.RequestApprovalParticipantId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_RequestApprovalDecision_RequestApprovalParticipant_RequestApprovalParticipantId");

        builder.HasIndex(x => x.RequestApprovalParticipantId)
            .IsUnique()
            .HasDatabaseName("UX_RequestApprovalDecision_Participant");

        builder.HasIndex(x => x.ClientDecisionId)
            .IsUnique()
            .HasDatabaseName("UX_RequestApprovalDecision_ClientId");
    }
}
