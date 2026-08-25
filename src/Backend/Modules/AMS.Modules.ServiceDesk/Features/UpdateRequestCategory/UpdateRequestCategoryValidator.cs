using FluentValidation;

namespace AMS.Modules.ServiceDesk.Features.UpdateRequestCategory;

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
public sealed class UpdateRequestCategoryValidator : AbstractValidator<UpdateRequestCategoryRequest>
{
    public UpdateRequestCategoryValidator()
    {
        RuleFor(x => x.CategoryName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CategoryType).NotEmpty().MaximumLength(20);
    }
}
