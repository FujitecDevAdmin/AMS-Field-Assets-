namespace AMS.Modules.Organization.Features.CreateLocation;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateLocationRequest(
    string LocationCode,
    string LocationName,
    int? RegionId,
    string TimeZoneId,
    bool IsHeadOffice);
