namespace AMS.Modules.Assets.Features.SearchAssets;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchAssetsMapper
{
    public static SearchAssetsQuery ToQuery(SearchAssetsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchAssetsQuery(
            string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            request.AssetTypeId,
            request.AssetClassId,
            request.AssetStatusId,
            request.LocationId,
            request.EmployeeId,
            request.DepartmentId,
            string.IsNullOrWhiteSpace(request.CostCenter) ? null : request.CostCenter.Trim(),
            string.IsNullOrWhiteSpace(request.SapAssetNumber) ? null : request.SapAssetNumber.Trim(),
            string.IsNullOrWhiteSpace(request.SapPlant) ? null : request.SapPlant.Trim(),
            request.AcquiredFrom,
            request.AcquiredTo,
            request.IsBulk,
            request.IsVerified,
            request.IncludeDeleted ?? false,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
