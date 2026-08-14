using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AssetReturnImage]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetReturnImageConfiguration : IEntityTypeConfiguration<AssetReturnImage>
{
    public void Configure(EntityTypeBuilder<AssetReturnImage> builder)
    {
        builder.ToTable("AssetReturnImage");

        builder.HasKey(x => x.Id).HasName("PK_AssetReturnImage");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AllocationId).IsRequired();
        builder.Property(x => x.ImagePath).HasMaxLength(400).IsRequired();
        builder.Property(x => x.Caption).HasMaxLength(200);
        builder.Property(x => x.ContentType).HasMaxLength(120);
        builder.Property(x => x.CapturedOnUtc).IsRequired();

        builder.HasOne<AssetAllocation>()
            .WithMany()
            .HasForeignKey(x => x.AllocationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetReturnImage_AssetAllocation_AllocationId");

        builder.HasOne<AssetHandover>()
            .WithMany()
            .HasForeignKey(x => x.HandoverId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetReturnImage_AssetHandover_HandoverId");

        builder.HasIndex(x => x.AllocationId)
            .HasDatabaseName("IX_AssetReturnImage_AllocationId");

        builder.HasIndex(x => x.HandoverId)
            .HasDatabaseName("IX_AssetReturnImage_HandoverId");
    }
}
