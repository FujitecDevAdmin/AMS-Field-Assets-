using FluentValidation;

namespace AMS.Modules.Movements.Features.DespatchBatch;

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
public sealed class DespatchBatchValidator : AbstractValidator<DespatchBatchRequest>
{
    public DespatchBatchValidator()
    {
        RuleFor(x => x.MovementType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.FromLocationId).GreaterThan(0);
        RuleFor(x => x.ToLocationId).GreaterThan(0);
        RuleFor(x => x.ToLocationId).NotEqual(x => x.FromLocationId)
            .WithMessage("A consignment cannot be sent to the branch it is leaving.");
        // Held once on the consignment rather than repeated on each asset:
        // three rows carrying one invoice number is three chances to edit one.
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(80);
        RuleFor(x => x.CourierName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TrackingNumber).MaximumLength(80);
        RuleFor(x => x.ChallanNumber).MaximumLength(80);
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}
