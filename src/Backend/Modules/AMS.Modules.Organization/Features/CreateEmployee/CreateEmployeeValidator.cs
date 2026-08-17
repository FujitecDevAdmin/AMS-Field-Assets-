using FluentValidation;

namespace AMS.Modules.Organization.Features.CreateEmployee;

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
public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.DepartmentId).GreaterThan(0).When(x => x.DepartmentId.HasValue);
        RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue);
        RuleFor(x => x.ReportingManagerId).GreaterThan(0).When(x => x.ReportingManagerId.HasValue);
    }
}
