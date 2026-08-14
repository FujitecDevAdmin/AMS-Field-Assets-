using FluentValidation;

namespace AMS.Modules.Allocations.Features.UpdateCustomerSite;

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
public sealed class UpdateCustomerSiteValidator : AbstractValidator<UpdateCustomerSiteRequest>
{
    public UpdateCustomerSiteValidator()
    {
        RuleFor(x => x.CustomerName).MaximumLength(200);
        RuleFor(x => x.SiteName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
    }
}
