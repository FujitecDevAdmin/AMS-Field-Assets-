using FluentValidation;

namespace AMS.Modules.Identity.Features.SetRoleCapabilities;

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
public sealed class SetRoleCapabilitiesValidator : AbstractValidator<SetRoleCapabilitiesRequest>
{
    public SetRoleCapabilitiesValidator()
    {
        RuleFor(x => x.CapabilityNames).NotNull();
        RuleForEach(x => x.CapabilityNames).NotEmpty().MaximumLength(80);
    }
}
