using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AssetAllocation]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetAllocationConfiguration : IEntityTypeConfiguration<AssetAllocation>
{
    public void Configure(EntityTypeBuilder<AssetAllocation> builder)
    {
        builder.ToTable("AssetAllocation");

        builder.HasKey(x => x.Id).HasName("PK_AssetAllocation");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.EmployeeId).IsRequired();
        builder.Property(x => x.AllocatedOnUtc).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => x.AssetId)
            .IsUnique()
            .HasFilter("[ReturnedOnUtc] IS NULL")
            .HasDatabaseName("UX_AssetAllocation_OneActivePerAsset");

        builder.HasIndex(x => new { x.LocationId, x.EmployeeId })
            .HasDatabaseName("IX_AssetAllocation_LocationEmployee");

        builder.HasIndex(x => x.ExpectedReturnDate)
            .HasFilter("[ReturnedOnUtc] IS NULL")
            .HasDatabaseName("IX_AssetAllocation_Overdue");
    }
}
