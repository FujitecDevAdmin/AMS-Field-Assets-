namespace AMS.Modules.Verification.Domain;

/// <summary>
/// Mirrors <c>[Verification].[PhysicalVerification]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class PhysicalVerification
{
    public int Id { get; set; }

    public int PhysicalVerificationCycleId { get; set; }

    public int AssetId { get; set; }

    public Guid? ClientCaptureId { get; set; }

    public bool IsBulkCount { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal? ExpectedQuantitySnapshot { get; set; }

    public string? ScannedQrValue { get; set; }

    public bool HasQrMismatch { get; set; }

    public required string WorkingCondition { get; set; }

    public bool SerialVerified { get; set; }

    public decimal? GpsLatitude { get; set; }

    public decimal? GpsLongitude { get; set; }

    public decimal? GpsAccuracyMetres { get; set; }

    public decimal? ReferenceLatitude { get; set; }

    public decimal? ReferenceLongitude { get; set; }

    public decimal? DistanceFromLocationMetres { get; set; }

    public decimal? AllowedRadiusMetres { get; set; }

    public string? GpsValidationStatus { get; set; }

    public bool HasLocationMismatch { get; set; }

    public bool? IsMockLocation { get; set; }

    public string? GpsValidationMessage { get; set; }

    public string? PhotoPath { get; set; }

    public int? LocationId { get; set; }

    public int? HolderEmployeeId { get; set; }

    public int? StatusUpdatedToId { get; set; }

    public int VerifiedByUserId { get; set; }

    public DateTime VerifiedOnUtc { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
