namespace AMS.Modules.Organization.Features.UpdateLocation;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateLocationMapper
{
    public static UpdateLocationCommand ToCommand(UpdateLocationRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateLocationCommand(
            id,
            request.LocationCode.Trim().ToUpperInvariant(),
            request.LocationName.Trim(),
            request.RegionId,
            request.TimeZoneId.Trim(),
            request.IsHeadOffice,
            request.IsActive);
    }
}
