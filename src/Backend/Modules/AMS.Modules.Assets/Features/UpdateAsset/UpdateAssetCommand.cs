using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateAsset;

/// <summary>
/// Edit an asset already on the register.
/// </summary>
public sealed record UpdateAssetCommand(
    int Id,
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
    string? Remarks) : ICommand<UpdateAssetResponse>;
