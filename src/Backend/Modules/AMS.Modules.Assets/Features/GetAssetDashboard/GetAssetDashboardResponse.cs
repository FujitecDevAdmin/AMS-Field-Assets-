namespace AMS.Modules.Assets.Features.GetAssetDashboard;

public sealed record GetAssetDashboardResponse(
    int TotalAssets,
    int VerifiedAssets,
    int PendingVerification,
    int MissingAssets,
    int EmployeeMappedAssets,
    int UnmappedAssets,
    int AssetsUnderRepair,
    int DisposedAssets,
    decimal TotalAssetValue,
    DateTime GeneratedOnUtc,
    IReadOnlyList<AssetDashboardBreakdown> AssetValueByLocation,
    IReadOnlyList<AssetDashboardBreakdown> AssetValueByDepartment,
    IReadOnlyList<AssetDashboardBreakdown> AssetsByStatus,
    IReadOnlyList<AssetDashboardBreakdown> AssetsByType,
    IReadOnlyList<AssetDashboardTrendPoint> AssetTrend,
    IReadOnlyList<AssetDashboardRecentAsset> RecentAssets);

public sealed record AssetDashboardBreakdown(string Name, decimal Value, int Count);

public sealed record AssetDashboardTrendPoint(string Period, int Added, int Verified);

public sealed record AssetDashboardRecentAsset(
    int Id,
    string AssetNumber,
    string AssetName,
    string Status,
    string Location,
    DateTime CreatedOnUtc);
