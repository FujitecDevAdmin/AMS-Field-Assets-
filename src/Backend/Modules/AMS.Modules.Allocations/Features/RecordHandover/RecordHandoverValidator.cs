using FluentValidation;

namespace AMS.Modules.Allocations.Features.RecordHandover;

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
public sealed class RecordHandoverValidator : AbstractValidator<RecordHandoverRequest>
{
    public RecordHandoverValidator()
    {
        RuleFor(x => x.BranchLocationId).GreaterThan(0);
        RuleFor(x => x.ReturnCondition).NotEmpty().MaximumLength(20);
        // Mandatory, and the design says why: "returned" without a condition
        // is the row that starts the argument six months later about who
        // broke the hinge.
        RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);
    }
}
