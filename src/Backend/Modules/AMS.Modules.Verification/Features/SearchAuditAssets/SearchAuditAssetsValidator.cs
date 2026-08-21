using FluentValidation;

namespace AMS.Modules.Verification.Features.SearchAuditAssets;

public sealed class SearchAuditAssetsValidator : AbstractValidator<SearchAuditAssetsRequest>
{
    public SearchAuditAssetsValidator() => RuleFor(request => request.AuditId).GreaterThan(0);
}
