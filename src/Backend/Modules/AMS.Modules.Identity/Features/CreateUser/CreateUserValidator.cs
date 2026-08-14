using FluentValidation;

namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// Shape only. Lengths mirror the schema exactly (docs/02 §5).
/// </summary>
/// <remarks>
/// What is deliberately NOT here: "username is not already taken". That is
/// <c>UX_User_Username</c>'s job. A read-then-write check is a race with a
/// nicer error message, and 03 §1 rule 6 forbids it — catch 2601/2627 and
/// return 409 instead.
/// </remarks>
public sealed class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9._@-]+$")
            .WithMessage("Username may contain letters, digits and . _ @ - only.");

        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(256);

        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));

        // A user who sees every branch must not also carry a branch list; the
        // two say different things and one of them would be ignored.
        RuleFor(x => x.BranchIds)
            .Must(b => b is null || b.Count == 0)
            .When(x => x.HasAllBranches)
            .WithMessage("A user with all-branch access must not also list branches.");

        RuleFor(x => x.PrimaryBranchId)
            .Must((request, primary) => primary is null || request.BranchIds?.Contains(primary.Value) == true)
            .WithMessage("The primary branch must be one of the branches granted.");
    }
}
