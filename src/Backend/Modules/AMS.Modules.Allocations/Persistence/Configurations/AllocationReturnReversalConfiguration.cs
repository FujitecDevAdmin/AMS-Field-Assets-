using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AllocationReturnReversal]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AllocationReturnReversalConfiguration : IEntityTypeConfiguration<AllocationReturnReversal>
{
    public void Configure(EntityTypeBuilder<AllocationReturnReversal> builder)
    {
        builder.ToTable("AllocationReturnReversal");

        builder.HasKey(x => x.Id).HasName("PK_AllocationReturnReversal");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AllocationId).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PreviousReturnedOnUtc).IsRequired();
        builder.Property(x => x.RestoredEmployeeId).IsRequired();
        builder.Property(x => x.ReversedByUserId).IsRequired();
        builder.Property(x => x.ReversedOnUtc).IsRequired();

        builder.HasOne<AssetAllocation>()
            .WithMany()
            .HasForeignKey(x => x.AllocationId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AllocationReturnReversal_AssetAllocation_AllocationId");

        builder.HasOne<AssetHandover>()
            .WithMany()
            .HasForeignKey(x => x.HandoverId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AllocationReturnReversal_AssetHandover_HandoverId");

        builder.HasIndex(x => x.AllocationId)
            .HasDatabaseName("IX_AllocationReturnReversal_AllocationId");
    }
}
