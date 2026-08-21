using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;

namespace AMS.Modules.Verification.Features.CalculateAuditAssetCount;

public sealed class CalculateAuditAssetCountHandler(
    IAssetSnapshot assets,
    IBranchDirectory branches)
    : IRequestHandler<CalculateAuditAssetCountQuery, CalculateAuditAssetCountResponse>
{
    public async Task<Result<CalculateAuditAssetCountResponse>> HandleAsync(
        CalculateAuditAssetCountQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var selectedBranches = await branches.FindActiveAsync(request.LocationBranchIds, ct);
        if (selectedBranches.Count != request.LocationBranchIds.Count)
        {
            return Error.Validation(
                "VerificationCycle.Locations",
                "Every audit location must be an active Branch Master record.");
        }

        var aliases = selectedBranches
            .SelectMany(branch => new[] { branch.BranchCode, branch.BranchName })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var total = await assets.CountByImportedBranchesAsync(request.LocationBranchIds, aliases, ct);

        return new CalculateAuditAssetCountResponse(total);
    }
}
