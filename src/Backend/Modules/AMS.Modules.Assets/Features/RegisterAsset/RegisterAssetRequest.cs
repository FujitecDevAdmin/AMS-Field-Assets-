namespace AMS.Modules.Assets.Features.RegisterAsset;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record RegisterAssetRequest(
    string AssetNumber,
    string AssetName,
    string? SerialNumber,
    int AssetTypeId,
    int? AssetClassId,
    string? Make,
    string? Model,
    int AssetStatusId,
    int? CurrentLocationId,
    int? DepartmentId,
    string? CostCenter,
    DateOnly? AcquisitionDate,
    bool? IsBulk,
    decimal? Quantity,
    string? UnitOfMeasure,
    string? Remarks);
