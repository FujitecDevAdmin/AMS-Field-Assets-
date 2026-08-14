using FluentValidation;

namespace AMS.Modules.ServiceDesk.Features.DecideApproval;

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
public sealed class DecideApprovalValidator : AbstractValidator<DecideApprovalRequest>
{
    public DecideApprovalValidator()
    {
        RuleFor(x => x.Remarks).MaximumLength(1000);
        RuleFor(x => x.Source).MaximumLength(20);
    }
}
