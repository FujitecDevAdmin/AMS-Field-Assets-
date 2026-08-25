namespace AMS.Modules.Organization.Features.UpdateBranch;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateBranchRequest(
    string BranchCode,
    string BranchName,
    int? RegionId,
    decimal? Latitude,
    decimal? Longitude,
    string TimeZoneId,
    bool IsHeadOffice,
    bool IsActive);
