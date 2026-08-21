using AMS.Modules.Verification.Domain;

namespace AMS.Modules.Verification.Features.SubmitVerification;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SubmitVerificationMapper
{
    public static SubmitVerificationCommand ToCommand(SubmitVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SubmitVerificationCommand(
            request.CycleId,
            request.AssetId,
            request.ClientCaptureId,
            request.IsBulkCount ?? false,
            request.CountedQuantity,
            request.ExpectedQuantitySnapshot,
            string.IsNullOrWhiteSpace(request.ScannedQrValue) ? null : request.ScannedQrValue.Trim(),
            string.IsNullOrWhiteSpace(request.WorkingCondition) ? WorkingCondition.Good : request.WorkingCondition.Trim(),
            request.SerialVerified ?? false,
            request.GpsLatitude,
            request.GpsLongitude,
            string.IsNullOrWhiteSpace(request.PhotoPath) ? null : request.PhotoPath.Trim(),
            request.LocationId,
            request.HolderEmployeeId,
            request.VerifiedOnUtc,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
