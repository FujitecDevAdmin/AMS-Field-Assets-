using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetDisposal]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetDisposalConfiguration : IEntityTypeConfiguration<AssetDisposal>
{
    public void Configure(EntityTypeBuilder<AssetDisposal> builder)
    {
        builder.ToTable("AssetDisposal", table =>
        {
            table.HasCheckConstraint("CK_AssetDisposal_QuantityPositive", "([DisposalQuantity] > 0)");
        });

        builder.HasKey(x => x.Id).HasName("PK_AssetDisposal");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.DisposalDate).IsRequired();
        builder.Property(x => x.DisposalQuantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.DisposalGrossValue).HasPrecision(18, 2);
        builder.Property(x => x.SaleProceeds).HasPrecision(18, 2);
        builder.Property(x => x.DisposalReason).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetDisposal_Asset_AssetId");

        builder.HasIndex(x => x.AssetId)
            .HasDatabaseName("IX_AssetDisposal_AssetId");
    }
}
