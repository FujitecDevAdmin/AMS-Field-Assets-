using AMS.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Contracts.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Contracts].[ContractReminderLog]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ContractReminderLogConfiguration : IEntityTypeConfiguration<ContractReminderLog>
{
    public void Configure(EntityTypeBuilder<ContractReminderLog> builder)
    {
        builder.ToTable("ContractReminderLog", table =>
        {
            table.HasCheckConstraint("CK_ContractReminderLog_Outcome", "([Outcome] IN (N'Queued', N'Sent', N'Failed'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_ContractReminderLog");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ContractId).IsRequired();
        builder.Property(x => x.DaysBeforeExpiry).IsRequired();
        builder.Property(x => x.ExpiryDateSnapshot).IsRequired();
        builder.Property(x => x.SentOnDate).IsRequired();
        builder.Property(x => x.SentTo).HasMaxLength(400);
        builder.Property(x => x.Outcome).HasMaxLength(20).IsRequired().HasDefaultValueSql("N'Queued'", "DF_ContractReminderLog_Outcome").ValueGeneratedNever();

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ContractReminderLog_Contract_ContractId");

        builder.HasIndex(x => new { x.ContractId, x.DaysBeforeExpiry, x.ExpiryDateSnapshot })
            .IsUnique()
            .HasFilter("[Outcome] <> N'Failed'")
            .HasDatabaseName("UX_ContractReminderLog_OncePerThreshold");
    }
}
