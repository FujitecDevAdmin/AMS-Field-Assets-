using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AssetHandover]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetHandoverConfiguration : IEntityTypeConfiguration<AssetHandover>
{
    public void Configure(EntityTypeBuilder<AssetHandover> builder)
    {
        builder.ToTable("AssetHandover", table =>
        {
            table.HasCheckConstraint("CK_AssetHandover_Status", "([Status] IN (N'HandedOver', N'InTransitToHo', N'ReceivedAtHo', N'Cancelled'))");
            table.HasCheckConstraint("CK_AssetHandover_Condition", "([ReturnCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing'))");
            table.HasCheckConstraint("CK_AssetHandover_ReceiptPair", "(([IsReceivedByHo] = 0 AND [ReceivedAtHoOnUtc] IS NULL) OR ([IsReceivedByHo] = 1 AND [ReceivedAtHoOnUtc] IS NOT NULL))");
            table.HasCheckConstraint("CK_AssetHandover_ReceiptStatus", "(([Status] = N'ReceivedAtHo' AND [IsReceivedByHo] = 1) OR ([Status] <> N'ReceivedAtHo' AND [IsReceivedByHo] = 0))");
            table.HasCheckConstraint("CK_AssetHandover_CancelPair", "(([Status] = N'Cancelled' AND [CancelledOnUtc] IS NOT NULL) OR ([Status] <> N'Cancelled' AND [CancelledOnUtc] IS NULL))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetHandover");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AllocationId).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.FromEmployeeId).IsRequired();
        builder.Property(x => x.BranchLocationId).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReturnCondition).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
        builder.Property(x => x.HandedOverOnUtc).IsRequired();
        builder.Property(x => x.ReceivedByUserId).IsRequired();
        builder.Property(x => x.IsReceivedByHo).IsRequired().HasDefaultValueSql("0", "DF_AssetHandover_IsReceivedByHo").ValueGeneratedNever();
        builder.Property(x => x.ReceiptRemarks).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<AssetAllocation>()
            .WithMany()
            .HasForeignKey(x => x.AllocationId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetHandover_AssetAllocation_AllocationId");

        builder.HasIndex(x => x.AssetId)
            .IsUnique()
            .HasFilter("[Status] = N'HandedOver'")
            .HasDatabaseName("UX_AssetHandover_OneOpenPerAsset");

        builder.HasIndex(x => x.AllocationId)
            .IsUnique()
            .HasFilter("[CancelledOnUtc] IS NULL")
            .HasDatabaseName("UX_AssetHandover_OnePerAllocation");

        builder.HasIndex(x => new { x.BranchLocationId, x.Status })
            .HasDatabaseName("IX_AssetHandover_BranchQueue");

        builder.HasIndex(x => new { x.Status, x.DispatchedOnUtc })
            .HasFilter("[IsReceivedByHo] = 0")
            .HasDatabaseName("IX_AssetHandover_GrnQueue");
    }
}
