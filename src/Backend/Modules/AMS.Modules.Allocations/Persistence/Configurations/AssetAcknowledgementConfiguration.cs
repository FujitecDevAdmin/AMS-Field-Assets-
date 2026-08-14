using AMS.Modules.Allocations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Allocations.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Allocations].[AssetAcknowledgement]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetAcknowledgementConfiguration : IEntityTypeConfiguration<AssetAcknowledgement>
{
    public void Configure(EntityTypeBuilder<AssetAcknowledgement> builder)
    {
        builder.ToTable("AssetAcknowledgement", table =>
        {
            table.HasCheckConstraint("CK_AssetAcknowledgement_Status", "([Status] IN (N'Pending', N'Signed', N'Approved'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetAcknowledgement");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AllocationId).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DocumentPath).HasMaxLength(400);
        builder.Property(x => x.SignatureImagePath).HasMaxLength(400);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<AssetAllocation>()
            .WithMany()
            .HasForeignKey(x => x.AllocationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetAcknowledgement_AssetAllocation_AllocationId");

        builder.HasIndex(x => x.AllocationId)
            .IsUnique()
            .HasDatabaseName("UX_AssetAcknowledgement_Allocation");
    }
}
