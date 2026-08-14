using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.SubmitVerification;

/// <summary>
/// Record a sighting or a bulk count. Catalogue: the mobile capture.
/// </summary>
public sealed record SubmitVerificationCommand(
    int AssetId,
    Guid? ClientCaptureId,
    bool IsBulkCount,
    decimal? CountedQuantity,
    decimal? ExpectedQuantitySnapshot,
    string? ScannedQrValue,
    string WorkingCondition,
    bool SerialVerified,
    decimal? GpsLatitude,
    decimal? GpsLongitude,
    string? PhotoPath,
    int? LocationId,
    int? HolderEmployeeId,
    DateTime? VerifiedOnUtc,
    string? Remarks) : ICommand<SubmitVerificationResponse>;
