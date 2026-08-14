using FluentValidation;

namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

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
public sealed class SetSoftwareCatalogEntryValidator : AbstractValidator<SetSoftwareCatalogEntryRequest>
{
    public SetSoftwareCatalogEntryValidator()
    {
        RuleFor(x => x.SoftwareName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Publisher).MaximumLength(200);
        RuleFor(x => x.LicensedSeats).GreaterThanOrEqualTo(0).When(x => x.LicensedSeats.HasValue);
    }
}
