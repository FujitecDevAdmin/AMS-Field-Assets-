using FluentValidation;

namespace AMS.Modules.Identity.Features.SetUserBranches;

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
public sealed class SetUserBranchesValidator : AbstractValidator<SetUserBranchesRequest>
{
    public SetUserBranchesValidator()
    {
        RuleFor(x => x.BranchIds).NotNull();
        RuleForEach(x => x.BranchIds).GreaterThan(0);
        RuleFor(x => x.PrimaryBranchId)
            .Must((request, primary) => primary is null || request.BranchIds.Contains(primary.Value))
            .WithMessage("The primary branch must be one of the branches granted.");
    }
}
