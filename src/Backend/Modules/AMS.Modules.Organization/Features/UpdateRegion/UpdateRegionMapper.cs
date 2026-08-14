namespace AMS.Modules.Organization.Features.UpdateRegion;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateRegionMapper
{
    public static UpdateRegionCommand ToCommand(UpdateRegionRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateRegionCommand(
            id,
            request.RegionName.Trim(),
            request.Description,
            request.IsActive);
    }
}
