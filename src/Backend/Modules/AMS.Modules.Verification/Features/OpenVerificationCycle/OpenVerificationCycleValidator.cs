using FluentValidation;

namespace AMS.Modules.Verification.Features.OpenVerificationCycle;

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
public sealed class OpenVerificationCycleValidator : AbstractValidator<OpenVerificationCycleRequest>
{
    public OpenVerificationCycleValidator()
    {
        RuleFor(x => x.CycleName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.AuditorUserIds).NotEmpty();
        RuleForEach(x => x.AuditorUserIds).GreaterThan(0);
        RuleFor(x => x.LocationBranchIds).NotEmpty();
        RuleForEach(x => x.LocationBranchIds).GreaterThan(0);
    }
}
