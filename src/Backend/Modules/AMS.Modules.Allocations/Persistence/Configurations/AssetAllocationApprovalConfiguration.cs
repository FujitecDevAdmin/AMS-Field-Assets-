using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AssetAllocationApproval]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetAllocationApprovalConfiguration : IEntityTypeConfiguration<AssetAllocationApproval>
{
    public void Configure(EntityTypeBuilder<AssetAllocationApproval> builder)
    {
        builder.ToTable("AssetAllocationApproval", table =>
        {
            table.HasCheckConstraint("CK_AssetAllocationApproval_Status", "([Status] IN (N'Pending', N'BranchApproved', N'Approved', N'Rejected', N'Cancelled'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetAllocationApproval");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RequestedByUserId).IsRequired();
        builder.Property(x => x.RequestedOnUtc).IsRequired();
        builder.Property(x => x.DecisionRemarks).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.Status, x.LocationId })
            .HasDatabaseName("IX_AssetAllocationApproval_Queue");
    }
}
