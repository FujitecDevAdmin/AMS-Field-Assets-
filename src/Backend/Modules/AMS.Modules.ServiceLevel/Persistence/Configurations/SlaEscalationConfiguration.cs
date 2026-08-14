using AMS.Modules.ServiceLevel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceLevel.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceLevel].[SlaEscalation]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SlaEscalationConfiguration : IEntityTypeConfiguration<SlaEscalation>
{
    public void Configure(EntityTypeBuilder<SlaEscalation> builder)
    {
        builder.ToTable("SlaEscalation", table =>
        {
            table.HasCheckConstraint("CK_SlaEscalation_Type", "([EscalationType] IN (N'Response', N'Resolution'))");
            table.HasCheckConstraint("CK_SlaEscalation_Level", "([Level] BETWEEN 1 AND 4)");
            table.HasCheckConstraint("CK_SlaEscalation_Threshold", "([ThresholdPercent] BETWEEN 1 AND 1000)");
            table.HasCheckConstraint("CK_SlaEscalation_Channel", "([Channel] IN (N'Email', N'InApp', N'Both'))");
            table.HasCheckConstraint("CK_SlaEscalation_RecipientType", "([RecipientType] IN (N'AssignedTechnician', N'TeamLead', N'BranchAdmin', N'Manager', N'Custom'))");
            table.HasCheckConstraint("CK_SlaEscalation_CustomAddress", "([RecipientType] <> N'Custom' OR [RecipientAddress] IS NOT NULL)");
        });

        builder.HasKey(x => x.Id).HasName("PK_SlaEscalation");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.SlaPolicyId).IsRequired();
        builder.Property(x => x.EscalationType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Level).IsRequired();
        builder.Property(x => x.ThresholdPercent).IsRequired();
        builder.Property(x => x.RecipientType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RecipientAddress).HasMaxLength(400);
        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<SlaPolicy>()
            .WithMany()
            .HasForeignKey(x => x.SlaPolicyId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_SlaEscalation_SlaPolicy_SlaPolicyId");

        builder.HasIndex(x => new { x.SlaPolicyId, x.EscalationType, x.Level })
            .IsUnique()
            .HasDatabaseName("UX_SlaEscalation_PolicyTypeLevel");
    }
}
