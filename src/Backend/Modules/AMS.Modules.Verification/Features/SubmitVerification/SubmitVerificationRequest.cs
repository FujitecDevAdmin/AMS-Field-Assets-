namespace AMS.Modules.Verification.Features.SubmitVerification;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SubmitVerificationRequest(
    int AssetId,
    Guid? ClientCaptureId,
    bool? IsBulkCount,
    decimal? CountedQuantity,
    decimal? ExpectedQuantitySnapshot,
    string? ScannedQrValue,
    string? WorkingCondition,
    bool? SerialVerified,
    decimal? GpsLatitude,
    decimal? GpsLongitude,
    string? PhotoPath,
    int? LocationId,
    int? HolderEmployeeId,
    DateTime? VerifiedOnUtc,
    string? Remarks);
