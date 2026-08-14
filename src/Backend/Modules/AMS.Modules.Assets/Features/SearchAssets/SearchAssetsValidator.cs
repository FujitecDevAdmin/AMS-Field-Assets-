using FluentValidation;

namespace AMS.Modules.Assets.Features.SearchAssets;

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
public sealed class SearchAssetsValidator : AbstractValidator<SearchAssetsRequest>
{
    public SearchAssetsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.CostCenter).MaximumLength(40);
        RuleFor(x => x.SapAssetNumber).MaximumLength(50);
        RuleFor(x => x.SapPlant).MaximumLength(20);
        RuleFor(x => x.AcquiredTo).GreaterThanOrEqualTo(x => x.AcquiredFrom)
            .When(x => x.AcquiredFrom.HasValue && x.AcquiredTo.HasValue);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
        // An unbounded page over 7,413 rows is a review-blocker (02 section 8).
        RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);
    }
}
