namespace AMS.Modules.Assets.Features.SetAssetCustomValues;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetAssetCustomValuesMapper
{
    public static SetAssetCustomValuesCommand ToCommand(SetAssetCustomValuesRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetAssetCustomValuesCommand(
            id,
            request.Values ?? []);
    }
}
