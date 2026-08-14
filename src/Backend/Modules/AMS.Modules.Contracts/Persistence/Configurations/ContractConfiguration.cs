using AMS.Modules.Contracts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Contracts.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Contracts].[Contract]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("Contract", table =>
        {
            table.HasCheckConstraint("CK_Contract_Window", "([EndDate] >= [StartDate])");
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("SysStartTime");
                temporal.HasPeriodEnd("SysEndTime");
                temporal.UseHistoryTable("ContractHistory", "Contracts");
            });
        });

        builder.HasKey(x => x.Id).HasName("PK_Contract");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ContractNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ContractName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContractType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.ContractValue).HasPrecision(18, 2);
        builder.Property(x => x.AutoRenew).IsRequired();
        builder.Property(x => x.RenewalCount).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(1000);
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        // R2-22: the token for a system-versioned table. SysStartTime is history only.
        builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken().HasDefaultValueSql("NEWID()", "DF_Contract_ConcurrencyStamp");

        builder.HasIndex(x => x.ContractNumber)
            .IsUnique()
            .HasDatabaseName("UX_Contract_Number");

        builder.HasIndex(x => x.EndDate)
            .HasDatabaseName("IX_Contract_EndDate");
    }
}
