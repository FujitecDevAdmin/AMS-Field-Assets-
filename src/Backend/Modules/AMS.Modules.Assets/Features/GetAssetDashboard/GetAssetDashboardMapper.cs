namespace AMS.Modules.Assets.Features.GetAssetDashboard;

public static class GetAssetDashboardMapper
{
    public static GetAssetDashboardQuery ToQuery(GetAssetDashboardRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new GetAssetDashboardQuery();
    }
}
