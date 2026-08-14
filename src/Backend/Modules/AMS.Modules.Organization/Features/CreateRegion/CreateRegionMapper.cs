namespace AMS.Modules.Organization.Features.CreateRegion;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateRegionMapper
{
    public static CreateRegionCommand ToCommand(CreateRegionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateRegionCommand(
            request.RegionName.Trim(),
            request.Description);
    }
}
