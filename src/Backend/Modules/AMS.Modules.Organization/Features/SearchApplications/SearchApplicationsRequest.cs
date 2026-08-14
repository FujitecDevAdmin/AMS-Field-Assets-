namespace AMS.Modules.Organization.Features.SearchApplications;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchApplicationsRequest(
    bool? IsActive,
    string? Search);
