using AMS.Modules.Transfers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Transfers.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Transfers].[AssetTransferRequest]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetTransferRequestConfiguration : IEntityTypeConfiguration<AssetTransferRequest>
{
    public void Configure(EntityTypeBuilder<AssetTransferRequest> builder)
    {
        builder.ToTable("AssetTransferRequest", table =>
        {
            table.HasCheckConstraint("CK_AssetTransferRequest_Status", "([Status] IN (N'Pending', N'Approved', N'Rejected', N'Completed', N'Cancelled'))");
            table.HasCheckConstraint("CK_AssetTransferRequest_SapSyncStatus", "([SapSyncStatus] IN (N'NotRequired', N'Pending', N'Sent', N'Failed'))");
            table.HasCheckConstraint("CK_AssetTransferRequest_TypePair", "(([TransferType] = 'Employee' AND [ToEmployeeId] IS NOT NULL) OR ([TransferType] = 'Department' AND [ToDepartmentId] IS NOT NULL) OR ([TransferType] = 'Branch' AND [ToLocationId] IS NOT NULL) OR ([TransferType] = 'CostCenter' AND [ToCostCenter] IS NOT NULL))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetTransferRequest");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.TransferType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FromCostCenter).HasMaxLength(40);
        builder.Property(x => x.ToCostCenter).HasMaxLength(40);
        builder.Property(x => x.RequestedByUserId).IsRequired();
        builder.Property(x => x.RequestedOnUtc).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.SapSyncStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.Status, x.FromLocationId })
            .HasDatabaseName("IX_AssetTransferRequest_Queue");

        builder.HasIndex(x => x.SapSyncStatus)
            .HasFilter("[SapSyncStatus] = 'Pending'")
            .HasDatabaseName("IX_AssetTransferRequest_SapPending");
    }
}
