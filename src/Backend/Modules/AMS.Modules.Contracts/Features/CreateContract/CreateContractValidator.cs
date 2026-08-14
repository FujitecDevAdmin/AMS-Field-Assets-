using FluentValidation;

namespace AMS.Modules.Contracts.Features.CreateContract;

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
public sealed class CreateContractValidator : AbstractValidator<CreateContractRequest>
{
    public CreateContractValidator()
    {
        RuleFor(x => x.ContractNumber).NotEmpty().MaximumLength(40);
        RuleFor(x => x.ContractName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContractType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ContractValue).GreaterThanOrEqualTo(0).When(x => x.ContractValue.HasValue);
        RuleFor(x => x.LicensedSeats).GreaterThan(0).When(x => x.LicensedSeats.HasValue);
        RuleFor(x => x.Remarks).MaximumLength(1000);
    }
}
