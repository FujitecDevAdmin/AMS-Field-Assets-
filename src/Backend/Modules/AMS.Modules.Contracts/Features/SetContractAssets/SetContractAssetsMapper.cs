namespace AMS.Modules.Contracts.Features.SetContractAssets;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetContractAssetsMapper
{
    public static SetContractAssetsCommand ToCommand(SetContractAssetsRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetContractAssetsCommand(
            id,
            request.AssetIds);
    }
}
