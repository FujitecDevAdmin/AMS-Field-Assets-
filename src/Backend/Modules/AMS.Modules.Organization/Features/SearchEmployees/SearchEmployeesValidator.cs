using FluentValidation;

namespace AMS.Modules.Organization.Features.SearchEmployees;

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
public sealed class SearchEmployeesValidator : AbstractValidator<SearchEmployeesRequest>
{
    public SearchEmployeesValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);
        // An unbounded employee list is a review-blocker (02 §8).
        RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);
    }
}
