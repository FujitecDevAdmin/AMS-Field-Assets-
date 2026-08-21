using FluentValidation;

namespace AMS.Modules.Verification.Features.AddAuditorsToCycle;

public sealed class AddAuditorsToCycleValidator : AbstractValidator<AddAuditorsToCycleCommand>
{
    public AddAuditorsToCycleValidator()
    {
        RuleFor(x => x.CycleId).GreaterThan(0);
        RuleFor(x => x.AuditorUserIds).NotEmpty();
        RuleForEach(x => x.AuditorUserIds).GreaterThan(0);
    }
}
