using FluentValidation;

namespace AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

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
public sealed class CreateSlaPolicyValidator : AbstractValidator<CreateSlaPolicyRequest>
{
    public CreateSlaPolicyValidator()
    {
        RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Priority).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ResponseTargetMinutes).GreaterThan(0);
        RuleFor(x => x.ResolutionTargetMinutes).GreaterThan(0);
        RuleFor(x => x.NearDueWarningMinutes).GreaterThanOrEqualTo(0).When(x => x.NearDueWarningMinutes.HasValue);
    }
}
