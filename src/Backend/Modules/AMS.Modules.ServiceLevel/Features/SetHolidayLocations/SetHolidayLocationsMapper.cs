namespace AMS.Modules.ServiceLevel.Features.SetHolidayLocations;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetHolidayLocationsMapper
{
    public static SetHolidayLocationsCommand ToCommand(SetHolidayLocationsRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetHolidayLocationsCommand(
            id,
            request.LocationIds);
    }
}
