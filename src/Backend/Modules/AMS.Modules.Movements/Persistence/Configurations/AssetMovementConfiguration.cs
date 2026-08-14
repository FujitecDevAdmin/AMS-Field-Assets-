using AMS.Modules.Movements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Movements.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Movements].[AssetMovement]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetMovementConfiguration : IEntityTypeConfiguration<AssetMovement>
{
    public void Configure(EntityTypeBuilder<AssetMovement> builder)
    {
        builder.ToTable("AssetMovement", table =>
        {
            table.HasCheckConstraint("CK_AssetMovement_QuantityPositive", "([Quantity] > 0)");
            table.HasCheckConstraint("CK_AssetMovement_DifferentBranches", "([FromLocationId] <> [ToLocationId])");
            table.HasCheckConstraint("CK_AssetMovement_Type", "([MovementType] IN (N'Transfer', N'HandoverToHO'))");
            table.HasCheckConstraint("CK_AssetMovement_Status", "([Status] IN (N'InTransit', N'Received', N'Cancelled'))");
            table.HasCheckConstraint("CK_AssetMovement_ReceiptPair", "(([Status] = N'Received' AND [ReceivedOnUtc] IS NOT NULL) OR ([Status] <> N'Received' AND [ReceivedOnUtc] IS NULL))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetMovement");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired().HasDefaultValueSql("1", "DF_AssetMovement_Quantity").ValueGeneratedNever();
        builder.Property(x => x.MovementType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FromLocationId).IsRequired();
        builder.Property(x => x.ToLocationId).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CourierName).HasMaxLength(100);
        builder.Property(x => x.TrackingNumber).HasMaxLength(80);
        builder.Property(x => x.ChallanNumber).HasMaxLength(80);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(80);
        builder.Property(x => x.DocumentPath).HasMaxLength(400);
        builder.Property(x => x.ShippedOnUtc).IsRequired();
        builder.Property(x => x.ReceiptRemarks).HasMaxLength(500);
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<MovementBatch>()
            .WithMany()
            .HasForeignKey(x => x.MovementBatchId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetMovement_MovementBatch_MovementBatchId");

        builder.HasIndex(x => new { x.Status, x.ToLocationId })
            .HasDatabaseName("IX_AssetMovement_Incoming");

        builder.HasIndex(x => x.MovementBatchId)
            .HasDatabaseName("IX_AssetMovement_Batch");

        builder.HasIndex(x => x.HandoverId)
            .HasDatabaseName("IX_AssetMovement_Handover");
    }
}
