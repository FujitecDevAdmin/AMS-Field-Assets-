using AMS.Modules.Verification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Verification.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Verification].[PhysicalVerificationCycle]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class PhysicalVerificationCycleConfiguration : IEntityTypeConfiguration<PhysicalVerificationCycle>
{
    public void Configure(EntityTypeBuilder<PhysicalVerificationCycle> builder)
    {
        builder.ToTable("PhysicalVerificationCycle");

        builder.HasKey(x => x.Id).HasName("PK_PhysicalVerificationCycle");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.CycleName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.BranchId).IsRequired();
        builder.Property(x => x.TotalAssetCount).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => x.CycleName)
            .IsUnique()
            .HasDatabaseName("UX_PhysicalVerificationCycle_Name");

    }
}
