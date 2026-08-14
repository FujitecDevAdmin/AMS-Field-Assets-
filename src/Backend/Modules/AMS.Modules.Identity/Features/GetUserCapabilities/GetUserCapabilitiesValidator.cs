using FluentValidation;

namespace AMS.Modules.Identity.Features.GetUserCapabilities;

/// <summary>
/// Every Request has a validator, even a trivial one - the pipeline requires
/// it so nobody forgets when a field is added later (docs/02 §5).
/// </summary>
public sealed class GetUserCapabilitiesValidator : AbstractValidator<GetUserCapabilitiesRequest>
{
    public GetUserCapabilitiesValidator() => RuleFor(x => x.UserId).GreaterThan(0);
}
