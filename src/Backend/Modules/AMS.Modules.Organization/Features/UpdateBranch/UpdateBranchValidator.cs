using FluentValidation;

namespace AMS.Modules.Organization.Features.UpdateBranch;

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
public sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchValidator()
    {
        RuleFor(x => x.BranchCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BranchName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.RegionId).GreaterThan(0).When(x => x.RegionId.HasValue);
    }
}
