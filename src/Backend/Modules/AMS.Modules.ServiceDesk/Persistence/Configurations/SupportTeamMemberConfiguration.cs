using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[SupportTeamMember]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SupportTeamMemberConfiguration : IEntityTypeConfiguration<SupportTeamMember>
{
    public void Configure(EntityTypeBuilder<SupportTeamMember> builder)
    {
        builder.ToTable("SupportTeamMember");

        builder.HasKey(x => new { x.SupportTeamId, x.UserId }).HasName("PK_SupportTeamMember");

        builder.Property(x => x.SupportTeamId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.IsLead).IsRequired();
        builder.Property(x => x.AddedOnUtc).IsRequired();

        builder.HasOne<SupportTeam>()
            .WithMany()
            .HasForeignKey(x => x.SupportTeamId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_SupportTeamMember_SupportTeam_SupportTeamId");
    }
}
