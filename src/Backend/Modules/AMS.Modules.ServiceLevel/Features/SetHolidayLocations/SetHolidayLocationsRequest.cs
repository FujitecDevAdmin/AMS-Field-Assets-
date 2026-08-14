namespace AMS.Modules.ServiceLevel.Features.SetHolidayLocations;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetHolidayLocationsRequest(
    IReadOnlyList<int> LocationIds);
