using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.ServiceDesk.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[ServiceDesk].[SupportTeam]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class SupportTeamConfiguration : IEntityTypeConfiguration<SupportTeam>
{
    public void Configure(EntityTypeBuilder<SupportTeam> builder)
    {
        builder.ToTable("SupportTeam");

        builder.HasKey(x => x.Id).HasName("PK_SupportTeam");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.TeamName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MailboxAddress).HasMaxLength(256);
        builder.Property(x => x.IsDefaultTeam).IsRequired().HasDefaultValueSql("0", "DF_SupportTeam_IsDefaultTeam").ValueGeneratedNever();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.TeamName)
            .IsUnique()
            .HasDatabaseName("UX_SupportTeam_Name");

        builder.HasIndex(x => x.IsDefaultTeam)
            .IsUnique()
            .HasFilter("[IsDefaultTeam] = 1")
            .HasDatabaseName("UX_SupportTeam_OneDefault");

        builder.HasIndex(x => x.RegionId)
            .HasDatabaseName("IX_SupportTeam_RegionId");
    }
}
