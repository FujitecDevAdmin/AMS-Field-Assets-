using AMS.Modules.Verification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Verification.Persistence.Configurations;

public sealed class PhysicalVerificationCycleLocationConfiguration : IEntityTypeConfiguration<PhysicalVerificationCycleLocation>
{
    public void Configure(EntityTypeBuilder<PhysicalVerificationCycleLocation> builder)
    {
        builder.ToTable("PhysicalVerificationCycleLocation");
        builder.HasKey(x => new { x.PhysicalVerificationCycleId, x.BranchId })
            .HasName("PK_PhysicalVerificationCycleLocation");
        builder.HasOne<PhysicalVerificationCycle>().WithMany()
            .HasForeignKey(x => x.PhysicalVerificationCycleId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_PhysicalVerificationCycleLocation_Cycle_PhysicalVerificationCycleId");
        builder.HasIndex(x => x.BranchId)
            .HasDatabaseName("IX_PhysicalVerificationCycleLocation_BranchId");
    }
}
