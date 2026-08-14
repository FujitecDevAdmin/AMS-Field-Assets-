namespace AMS.Modules.Assets.Features.CreateAssetClass;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateAssetClassMapper
{
    public static CreateAssetClassCommand ToCommand(CreateAssetClassRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateAssetClassCommand(
            request.ClassCode.Trim(),
            request.ClassName.Trim(),
            request.ReportingCategory.Trim(),
            request.IsDepreciable ?? true,
            request.IsIntangible ?? false);
    }
}
