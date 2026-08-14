using AMS.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Contracts.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Contracts].[ContractReminderSetting]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ContractReminderSettingConfiguration : IEntityTypeConfiguration<ContractReminderSetting>
{
    public void Configure(EntityTypeBuilder<ContractReminderSetting> builder)
    {
        builder.ToTable("ContractReminderSetting", table =>
        {
            table.HasCheckConstraint("CK_ContractReminderSetting_Days", "([DaysBeforeExpiry] BETWEEN 1 AND 365)");
            table.HasCheckConstraint("CK_ContractReminderSetting_Channel", "([Channel] IN (N'Email', N'InApp', N'Both'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_ContractReminderSetting");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.DaysBeforeExpiry).IsRequired();
        builder.Property(x => x.Recipients).HasMaxLength(1000);
        builder.Property(x => x.Channel).HasMaxLength(20).IsRequired().HasDefaultValueSql("N'Email'", "DF_ContractReminderSetting_Channel").ValueGeneratedNever();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(x => x.ContractId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_ContractReminderSetting_Contract_ContractId");

        builder.HasIndex(x => x.DaysBeforeExpiry)
            .IsUnique()
            .HasFilter("[ContractId] IS NULL")
            .HasDatabaseName("UX_ContractReminderSetting_Default");

        builder.HasIndex(x => new { x.ContractId, x.DaysBeforeExpiry })
            .IsUnique()
            .HasFilter("[ContractId] IS NOT NULL")
            .HasDatabaseName("UX_ContractReminderSetting_PerContract");
    }
}
