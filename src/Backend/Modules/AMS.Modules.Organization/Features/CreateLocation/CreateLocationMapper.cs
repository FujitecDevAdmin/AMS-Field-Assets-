namespace AMS.Modules.Organization.Features.CreateLocation;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateLocationMapper
{
    public static CreateLocationCommand ToCommand(CreateLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateLocationCommand(
            request.LocationCode.Trim().ToUpperInvariant(),
            request.LocationName.Trim(),
            request.RegionId,
            request.TimeZoneId.Trim(),
            request.IsHeadOffice);
    }
}
