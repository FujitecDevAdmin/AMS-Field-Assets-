using FluentValidation;

namespace AMS.Modules.Verification.Features.CalculateAuditAssetCount;

public sealed class CalculateAuditAssetCountValidator
    : AbstractValidator<CalculateAuditAssetCountRequest>
{
    public CalculateAuditAssetCountValidator()
    {
        RuleFor(x => x.LocationBranchIds).NotEmpty();
        RuleForEach(x => x.LocationBranchIds).GreaterThan(0);
    }
}
