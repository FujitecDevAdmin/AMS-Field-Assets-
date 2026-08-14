using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.RegisterAsset;

/// <summary>
/// Register an asset. Catalogue: Register an asset.
/// </summary>
public sealed record RegisterAssetCommand(
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
    bool IsBulk,
    decimal Quantity,
    string? UnitOfMeasure,
    string? Remarks) : ICommand<RegisterAssetResponse>;
