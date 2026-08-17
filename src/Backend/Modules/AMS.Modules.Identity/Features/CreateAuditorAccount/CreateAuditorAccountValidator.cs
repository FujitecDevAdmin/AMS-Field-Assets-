using FluentValidation;

namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public sealed class CreateAuditorAccountValidator : AbstractValidator<CreateAuditorAccountRequest>
{
    public CreateAuditorAccountValidator()
    {
        RuleFor(request => request.Username).NotEmpty().MaximumLength(100)
            .Matches("^[A-Za-z0-9._@-]+$");
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(12).MaximumLength(256);
        RuleFor(request => request.Email).MaximumLength(256).EmailAddress()
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.BranchIds).Must(ids => ids is null || ids.Count == 0)
            .When(request => request.HasAllBranches);
        RuleFor(request => request.PrimaryBranchId)
            .Must((request, primary) => primary is null || request.BranchIds?.Contains(primary.Value) == true);
    }
}
