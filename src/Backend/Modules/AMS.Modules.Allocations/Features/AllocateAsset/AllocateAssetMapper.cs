namespace AMS.Modules.Allocations.Features.AllocateAsset;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class AllocateAssetMapper
{
    public static AllocateAssetCommand ToCommand(AllocateAssetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AllocateAssetCommand(
            request.AssetId,
            request.EmployeeId,
            request.LocationId,
            request.ExpectedReturnDate,
            request.ApprovalId,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
