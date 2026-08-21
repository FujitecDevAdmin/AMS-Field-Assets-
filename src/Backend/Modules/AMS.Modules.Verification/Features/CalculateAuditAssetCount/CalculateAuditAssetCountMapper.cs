namespace AMS.Modules.Verification.Features.CalculateAuditAssetCount;

public static class CalculateAuditAssetCountMapper
{
    public static CalculateAuditAssetCountQuery ToQuery(CalculateAuditAssetCountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CalculateAuditAssetCountQuery(request.LocationBranchIds.Distinct().ToArray());
    }
}
