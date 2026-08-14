using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SearchAssets;

/// <summary>
/// The register grid. Catalogue screen: Asset Register.
/// </summary>
public sealed record SearchAssetsQuery(
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
    bool IncludeDeleted,
    int Skip,
    int Take) : IQuery<SearchAssetsResponse>;
