using FluentValidation;

namespace AMS.Modules.Verification.Features.SubmitVerification;

/// <summary>
/// Shape only. Lengths mirror the schema exactly.
/// </summary>
/// <remarks>
/// Business invariants are NOT here. "Already taken", "already allocated" and
/// "one active per X" are filtered unique indexes, and a read-then-write check
/// is a race with a nicer error message (docs/02 §5, 03 §1 rule 6).
///
/// Every Request has a validator, even a trivial one, so nobody forgets when a
/// field is added later.
/// </remarks>
public sealed class SubmitVerificationValidator : AbstractValidator<SubmitVerificationRequest>
{
    public SubmitVerificationValidator()
    {
        RuleFor(x => x.CycleId).GreaterThan(0);
        RuleFor(x => x.AssetId).GreaterThan(0);
        RuleFor(x => x.WorkingCondition).MaximumLength(20);
        // Scanner payloads may be URLs or encoded data and can legitimately be
        // longer than the persisted identifier. The handler resolves them to
        // the asset's configured QR/barcode/asset number before saving.
        RuleFor(x => x.PhotoPath).MaximumLength(400);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0).When(x => x.CountedQuantity.HasValue);
        RuleFor(x => x.GpsLatitude).InclusiveBetween(-90, 90).When(x => x.GpsLatitude.HasValue);
        RuleFor(x => x.GpsLongitude).InclusiveBetween(-180, 180).When(x => x.GpsLongitude.HasValue);
        RuleFor(x => x.GpsAccuracyMetres).GreaterThanOrEqualTo(0).When(x => x.GpsAccuracyMetres.HasValue);
    }
}
