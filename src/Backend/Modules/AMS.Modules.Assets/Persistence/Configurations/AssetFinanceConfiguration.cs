using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetFinance]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetFinanceConfiguration : IEntityTypeConfiguration<AssetFinance>
{
    public void Configure(EntityTypeBuilder<AssetFinance> builder)
    {
        builder.ToTable("AssetFinance", table =>
        {
            table.HasCheckConstraint("CK_AssetFinance_Method", "([DepreciationMethod] IS NULL OR [DepreciationMethod] IN (N'StraightLine', N'WrittenDownValue', N'None'))");
        });

        builder.HasKey(x => x.AssetId).HasName("PK_AssetFinance");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.OriginalValue).HasPrecision(18, 2);
        builder.Property(x => x.MigratedBookValue).HasPrecision(18, 2);
        builder.Property(x => x.AdditionalValue).HasPrecision(18, 2);
        builder.Property(x => x.GrossValue).HasPrecision(18, 2);
        builder.Property(x => x.DisposalGrossValue).HasPrecision(18, 2);
        builder.Property(x => x.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(x => x.NetBookValue).HasPrecision(18, 2);
        builder.Property(x => x.DepreciationMethod).HasMaxLength(30);
        builder.Property(x => x.DepreciationPercent).HasPrecision(9, 4);
        builder.Property(x => x.CapitalisedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.SapPostingStatus).HasMaxLength(20);
        builder.Property(x => x.AucReference).HasMaxLength(50);
        builder.Property(x => x.OpportunityName).HasMaxLength(200);
        builder.Property(x => x.VoucherNo).HasMaxLength(60);
        builder.Property(x => x.ApVoucherNo).HasMaxLength(60);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetFinance_Asset_AssetId");

        builder.HasOne<ChartOfAccount>()
            .WithMany()
            .HasForeignKey(x => x.GrossValueCoaId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetFinance_ChartOfAccount_GrossValueCoaId");

        builder.HasOne<ChartOfAccount>()
            .WithMany()
            .HasForeignKey(x => x.AccumulatedDepreciationCoaId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetFinance_ChartOfAccount_AccumulatedDepreciationCoaId");

        builder.HasOne<ChartOfAccount>()
            .WithMany()
            .HasForeignKey(x => x.DepreciationChargeCoaId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetFinance_ChartOfAccount_DepreciationChargeCoaId");
    }
}
