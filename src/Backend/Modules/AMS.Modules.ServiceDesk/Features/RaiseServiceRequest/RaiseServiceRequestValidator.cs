using FluentValidation;

namespace AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;

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
public sealed class RaiseServiceRequestValidator : AbstractValidator<RaiseServiceRequestRequest>
{
    public RaiseServiceRequestValidator()
    {
        RuleFor(x => x.RequestKind).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Priority).MaximumLength(20);
        RuleFor(x => x.ManualAssetText).MaximumLength(200);
        RuleFor(x => x.RequestedByEmployeeId).GreaterThan(0);
    }
}
