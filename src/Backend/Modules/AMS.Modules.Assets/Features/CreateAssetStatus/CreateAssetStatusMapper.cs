namespace AMS.Modules.Assets.Features.CreateAssetStatus;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateAssetStatusMapper
{
    public static CreateAssetStatusCommand ToCommand(CreateAssetStatusRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateAssetStatusCommand(
            request.StatusName.Trim(),
            request.IsTerminal,
            request.DisplayOrder ?? 0);
    }
}
