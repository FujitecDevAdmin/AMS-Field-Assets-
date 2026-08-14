using AMS.Modules.Assets.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Assets.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Assets].[AssetInstrumentDetail]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class AssetInstrumentDetailConfiguration : IEntityTypeConfiguration<AssetInstrumentDetail>
{
    public void Configure(EntityTypeBuilder<AssetInstrumentDetail> builder)
    {
        builder.ToTable("AssetInstrumentDetail", table =>
        {
            table.HasCheckConstraint("CK_AssetInstrumentDetail_Window", "([CalibrationEndDate] IS NULL OR [CalibrationStartDate] IS NULL OR [CalibrationEndDate] >= [CalibrationStartDate])");
        });

        builder.HasKey(x => x.AssetId).HasName("PK_AssetInstrumentDetail");
        builder.Property(x => x.AssetId).ValueGeneratedNever();

        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.CalibrationAgency).HasMaxLength(200);
        builder.Property(x => x.CertificateNumber).HasMaxLength(80);
        builder.Property(x => x.MeasurementRange).HasMaxLength(100);
        builder.Property(x => x.AccuracyClass).HasMaxLength(50);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_AssetInstrumentDetail_Asset_AssetId");

        builder.HasIndex(x => x.CalibrationEndDate)
            .HasDatabaseName("IX_AssetInstrumentDetail_CalibrationDue");
    }
}
