namespace AMS.Modules.Assets.Features.UpdateAssetClass;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateAssetClassMapper
{
    public static UpdateAssetClassCommand ToCommand(UpdateAssetClassRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateAssetClassCommand(
            id,
            request.ClassCode.Trim(),
            request.ClassName.Trim(),
            request.ReportingCategory.Trim(),
            request.IsDepreciable ?? true,
            request.IsIntangible ?? false,
            request.IsActive);
    }
}
