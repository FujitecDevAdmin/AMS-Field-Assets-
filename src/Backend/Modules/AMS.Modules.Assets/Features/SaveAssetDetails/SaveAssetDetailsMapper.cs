namespace AMS.Modules.Assets.Features.SaveAssetDetails;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SaveAssetDetailsMapper
{
    public static SaveAssetDetailsCommand ToCommand(SaveAssetDetailsRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SaveAssetDetailsCommand(
            id,
            request.Hardware,
            request.Software,
            request.Purchase,
            request.Vehicle,
            request.Instrument);
    }
}
