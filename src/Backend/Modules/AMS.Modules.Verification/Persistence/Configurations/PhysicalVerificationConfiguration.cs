using AMS.Modules.Verification.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AMS.Modules.Verification.Persistence.Configurations;

/// <summary>
/// Mirrors <c>[Verification].[PhysicalVerification]</c> in AMS_Consolidated_Design_v2.sql,
/// including every constraint and index NAME (docs/03 §3).
/// </summary>
public sealed class PhysicalVerificationConfiguration : IEntityTypeConfiguration<PhysicalVerification>
{
    public void Configure(EntityTypeBuilder<PhysicalVerification> builder)
    {
        builder.ToTable("PhysicalVerification", table =>
        {
            table.HasCheckConstraint("CK_PhysicalVerification_BulkHasCount", "([IsBulkCount] = 0 OR [CountedQuantity] IS NOT NULL)");
            table.HasCheckConstraint("CK_PhysicalVerification_CountNonNegative", "([CountedQuantity] IS NULL OR [CountedQuantity] >= 0)");
            table.HasCheckConstraint("CK_PhysicalVerification_Condition", "([WorkingCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing'))");
            table.HasCheckConstraint("CK_PhysicalVerification_GpsAccuracy", "([GpsAccuracyMetres] IS NULL OR [GpsAccuracyMetres] >= 0)");
            table.HasCheckConstraint("CK_PhysicalVerification_ReferenceLatitude", "([ReferenceLatitude] IS NULL OR ([ReferenceLatitude] >= -90 AND [ReferenceLatitude] <= 90))");
            table.HasCheckConstraint("CK_PhysicalVerification_ReferenceLongitude", "([ReferenceLongitude] IS NULL OR ([ReferenceLongitude] >= -180 AND [ReferenceLongitude] <= 180))");
            table.HasCheckConstraint("CK_PhysicalVerification_Distance", "([DistanceFromLocationMetres] IS NULL OR [DistanceFromLocationMetres] >= 0)");
            table.HasCheckConstraint("CK_PhysicalVerification_AllowedRadius", "([AllowedRadiusMetres] IS NULL OR [AllowedRadiusMetres] >= 0)");
            table.HasCheckConstraint("CK_PhysicalVerification_GpsValidationStatus", "([GpsValidationStatus] IS NULL OR [GpsValidationStatus] IN (N'NotValidated', N'InsideGeofence', N'OutsideGeofence', N'ReferenceUnavailable'))");
        });

        builder.HasKey(x => x.Id).HasName("PK_PhysicalVerification");

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.PhysicalVerificationCycleId).IsRequired();
        builder.Property(x => x.AssetId).IsRequired();
        builder.Property(x => x.IsBulkCount).IsRequired().HasDefaultValueSql("0", "DF_PhysicalVerification_IsBulkCount").ValueGeneratedNever();
        builder.Property(x => x.CountedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ExpectedQuantitySnapshot).HasPrecision(18, 3);
        builder.Property(x => x.ScannedQrValue).HasMaxLength(200);
        builder.Property(x => x.HasQrMismatch).IsRequired();
        builder.Property(x => x.WorkingCondition).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SerialVerified).IsRequired();
        builder.Property(x => x.GpsLatitude).HasPrecision(9, 6);
        builder.Property(x => x.GpsLongitude).HasPrecision(9, 6);
        builder.Property(x => x.GpsAccuracyMetres).HasPrecision(9, 2);
        builder.Property(x => x.ReferenceLatitude).HasPrecision(9, 6);
        builder.Property(x => x.ReferenceLongitude).HasPrecision(9, 6);
        builder.Property(x => x.DistanceFromLocationMetres).HasPrecision(12, 2);
        builder.Property(x => x.AllowedRadiusMetres).HasPrecision(12, 2);
        builder.Property(x => x.GpsValidationStatus).HasMaxLength(20);
        builder.Property(x => x.HasLocationMismatch).IsRequired().HasDefaultValueSql("0", "DF_PhysicalVerification_HasLocationMismatch").ValueGeneratedNever();
        builder.Property(x => x.GpsValidationMessage).HasMaxLength(500);
        builder.Property(x => x.PhotoPath).HasMaxLength(400);
        builder.Property(x => x.VerifiedByUserId).IsRequired();
        builder.Property(x => x.VerifiedOnUtc).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.CreatedOnUtc).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.ModifiedBy).HasMaxLength(100);

        builder.HasOne<PhysicalVerificationCycle>()
            .WithMany()
            .HasForeignKey(x => x.PhysicalVerificationCycleId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_PhysicalVerification_PhysicalVerificationCycle_PhysicalVerificationCycleId");

        builder.HasIndex(x => new { x.PhysicalVerificationCycleId, x.AssetId })
            .IsUnique()
            .HasFilter("[IsBulkCount] = 0")
            .HasDatabaseName("UX_PhysicalVerification_OnePerUnitAssetPerCycle");

        builder.HasIndex(x => new { x.PhysicalVerificationCycleId, x.AssetId, x.LocationId })
            .IsUnique()
            .HasFilter("[IsBulkCount] = 1")
            .HasDatabaseName("UX_PhysicalVerification_OneBulkCountPerPlacePerCycle");

        builder.HasIndex(x => new { x.LocationId, x.WorkingCondition })
            .HasDatabaseName("IX_PhysicalVerification_Exceptions");

        builder.HasIndex(x => x.ClientCaptureId)
            .IsUnique()
            .HasFilter("[ClientCaptureId] IS NOT NULL")
            .HasDatabaseName("UX_PhysicalVerification_ClientCapture");
    }
}
