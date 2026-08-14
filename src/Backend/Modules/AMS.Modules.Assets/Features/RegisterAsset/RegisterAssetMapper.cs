namespace AMS.Modules.Assets.Features.RegisterAsset;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RegisterAssetMapper
{
    public static RegisterAssetCommand ToCommand(RegisterAssetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RegisterAssetCommand(
            request.AssetNumber.Trim(),
            request.AssetName.Trim(),
            string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim(),
            request.AssetTypeId,
            request.AssetClassId,
            string.IsNullOrWhiteSpace(request.Make) ? null : request.Make.Trim(),
            string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            request.AssetStatusId,
            request.CurrentLocationId,
            request.DepartmentId,
            string.IsNullOrWhiteSpace(request.CostCenter) ? null : request.CostCenter.Trim(),
            request.AcquisitionDate,
            request.IsBulk ?? false,
            request.Quantity ?? 1m,
            string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? null : request.UnitOfMeasure.Trim(),
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
