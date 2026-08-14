using FluentValidation;

namespace AMS.Modules.Discovery.Features.ReportInventory;

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
public sealed class ReportInventoryValidator : AbstractValidator<ReportInventoryRequest>
{
    public ReportInventoryValidator()
    {
        RuleFor(x => x.Hostname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Manufacturer).MaximumLength(150);
        RuleFor(x => x.Model).MaximumLength(150);
        RuleFor(x => x.OperatingSystem).MaximumLength(150);
        RuleFor(x => x.MacAddress).MaximumLength(50);
    }
}
