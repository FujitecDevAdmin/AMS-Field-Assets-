using FluentValidation;

namespace AMS.Modules.Organization.Features.CreateLocation;

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
public sealed class CreateLocationValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationValidator()
    {
        RuleFor(x => x.LocationCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LocationName).NotEmpty().MaximumLength(100);
        // Not optional: a branch without a time zone cannot say what 09:00 means
        // there, and every SLA measurement taken against it would be wrong.
        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.RegionId).GreaterThan(0).When(x => x.RegionId.HasValue);
    }
}
