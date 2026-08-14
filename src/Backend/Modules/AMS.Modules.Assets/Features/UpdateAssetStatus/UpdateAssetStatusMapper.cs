namespace AMS.Modules.Assets.Features.UpdateAssetStatus;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateAssetStatusMapper
{
    public static UpdateAssetStatusCommand ToCommand(UpdateAssetStatusRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateAssetStatusCommand(
            id,
            request.StatusName.Trim(),
            request.IsTerminal,
            request.DisplayOrder ?? 0,
            request.IsActive);
    }
}
