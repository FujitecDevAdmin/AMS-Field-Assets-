using AMS.Modules.Verification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Verification.Persistence.Configurations;

public sealed class PhysicalVerificationAssignmentConfiguration : IEntityTypeConfiguration<PhysicalVerificationAssignment>
{
    public void Configure(EntityTypeBuilder<PhysicalVerificationAssignment> builder)
    {
        builder.ToTable("PhysicalVerificationAssignment");
        builder.HasKey(x => new { x.PhysicalVerificationCycleId, x.AuditorUserId })
            .HasName("PK_PhysicalVerificationAssignment");
        builder.Property(x => x.AssignedOnUtc).IsRequired();
        builder.Property(x => x.AssignedBy).HasMaxLength(100);
        builder.HasOne<PhysicalVerificationCycle>().WithMany()
            .HasForeignKey(x => x.PhysicalVerificationCycleId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_PhysicalVerificationAssignment_Cycle_PhysicalVerificationCycleId");
        builder.HasIndex(x => x.AuditorUserId)
            .HasDatabaseName("IX_PhysicalVerificationAssignment_AuditorUserId");
    }
}
