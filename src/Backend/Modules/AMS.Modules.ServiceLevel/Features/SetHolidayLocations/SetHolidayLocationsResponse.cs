namespace AMS.Modules.ServiceLevel.Features.SetHolidayLocations;

/// <summary>
/// How many branches observe it now.
/// </summary>
/// <param name="Id">The holiday.</param>
/// <param name="LocationCount">The branches attached to it.</param>
public sealed record SetHolidayLocationsResponse(
    int Id,
    int LocationCount);
