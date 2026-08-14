using AMS.Modules.Movements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Movements.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Movements].[MovementBatch]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class MovementBatchConfiguration : IEntityTypeConfiguration<MovementBatch>
{
    public void Configure(EntityTypeBuilder<MovementBatch> builder)
    {
        builder.ToTable("MovementBatch", table =>
        {
            table.HasCheckConstraint("CK_MovementBatch_DifferentBranches", "([FromLocationId] <> [ToLocationId])");
            table.HasCheckConstraint("CK_MovementBatch_PositiveCount", "([ItemCount] > 0)");
            table.HasCheckConstraint("CK_MovementBatch_Type", "([MovementType] IN (N'Transfer', N'HandoverToHO'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_MovementBatch");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.BatchNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FromLocationId).IsRequired();
        builder.Property(x => x.ToLocationId).IsRequired();
        builder.Property(x => x.MovementType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.InvoiceNumber).HasMaxLength(80).IsRequired();
        builder.Property(x => x.InvoiceDate).IsRequired();
        builder.Property(x => x.CourierName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TrackingNumber).HasMaxLength(80);
        builder.Property(x => x.ChallanNumber).HasMaxLength(80);
        builder.Property(x => x.DocumentPath).HasMaxLength(400);
        builder.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ItemCount).IsRequired();
        builder.Property(x => x.DispatchedByUserId).IsRequired();
        builder.Property(x => x.ShippedOnUtc).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.BatchNumber)
            .IsUnique()
            .HasDatabaseName("UX_MovementBatch_Number");

        builder.HasIndex(x => new { x.ToLocationId, x.ShippedOnUtc })
            .HasFilter("[ReceivedOnUtc] IS NULL")
            .HasDatabaseName("IX_MovementBatch_Open");
    }
}
