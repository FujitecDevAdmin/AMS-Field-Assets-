using FluentValidation;

namespace AMS.Modules.Assets.Features.CreateAuditorLocation;

public sealed class CreateAuditorLocationValidator : AbstractValidator<CreateAuditorLocationRequest>
{
    public CreateAuditorLocationValidator()
    {
        RuleFor(request => request.LocationName).NotEmpty().MaximumLength(150);
    }
}
