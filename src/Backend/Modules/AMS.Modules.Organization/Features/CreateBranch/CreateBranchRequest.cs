namespace AMS.Modules.Organization.Features.CreateBranch;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateBranchRequest(
    string BranchCode,
    string BranchName,
    int? RegionId,
    decimal? Latitude,
    decimal? Longitude,
    string TimeZoneId,
    bool IsHeadOffice);
