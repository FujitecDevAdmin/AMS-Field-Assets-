namespace AMS.Modules.Identity.Features.SearchUsers;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchUsersRequest(
    string? Search,
    bool? IsActive,
    int? Skip,
    int? Take);
