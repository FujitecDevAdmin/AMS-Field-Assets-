namespace AMS.Modules.Assets.Features.GetAssetTypeCustomFields;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetAssetTypeCustomFieldsMapper
{
    public static GetAssetTypeCustomFieldsQuery ToQuery(GetAssetTypeCustomFieldsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetAssetTypeCustomFieldsQuery(
            request.AssetTypeId,
            request.IncludeInactive ?? false);
    }
}
