namespace AMS.Modules.Organization.Features.UpdateLocation;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateLocationRequest(
    string LocationCode,
    string LocationName,
    int? RegionId,
    string TimeZoneId,
    bool IsHeadOffice,
    bool IsActive);
