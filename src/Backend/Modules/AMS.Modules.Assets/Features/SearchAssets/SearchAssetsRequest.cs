namespace AMS.Modules.Assets.Features.SearchAssets;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAssetsRequest(
    string? Search,
    int? AssetTypeId,
    int? AssetClassId,
    int? AssetStatusId,
    int? LocationId,
    int? EmployeeId,
    int? DepartmentId,
    string? CostCenter,
    string? SapAssetNumber,
    string? SapPlant,
    DateOnly? AcquiredFrom,
    DateOnly? AcquiredTo,
    bool? IsBulk,
    bool? IsVerified,
    bool? IncludeDeleted,
    int? Skip,
    int? Take);
