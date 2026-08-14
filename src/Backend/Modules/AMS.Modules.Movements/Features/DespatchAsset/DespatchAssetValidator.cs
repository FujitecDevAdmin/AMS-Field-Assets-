using FluentValidation;

namespace AMS.Modules.Movements.Features.DespatchAsset;

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
public sealed class DespatchAssetValidator : AbstractValidator<DespatchAssetRequest>
{
    public DespatchAssetValidator()
    {
        RuleFor(x => x.AssetId).GreaterThan(0);
        RuleFor(x => x.MovementType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FromLocationId).GreaterThan(0);
        RuleFor(x => x.ToLocationId).GreaterThan(0);
        // CK_AssetMovement_DifferentBranches says the same thing. Saying it
        // here turns a 500 into a message beside the field.
        RuleFor(x => x.ToLocationId).NotEqual(x => x.FromLocationId)
            .WithMessage("An asset cannot be sent to the branch it is leaving.");
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.CourierName).MaximumLength(100);
        RuleFor(x => x.TrackingNumber).MaximumLength(80);
        RuleFor(x => x.ChallanNumber).MaximumLength(80);
        RuleFor(x => x.InvoiceNumber).MaximumLength(80);
        RuleFor(x => x.Remarks).MaximumLength(500);
    }
}
