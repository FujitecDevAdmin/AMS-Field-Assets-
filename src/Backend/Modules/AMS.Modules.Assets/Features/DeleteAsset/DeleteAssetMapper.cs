namespace AMS.Modules.Assets.Features.DeleteAsset;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DeleteAssetMapper
{
    public static DeleteAssetCommand ToCommand(DeleteAssetRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DeleteAssetCommand(
            id,
            string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim());
    }
}
