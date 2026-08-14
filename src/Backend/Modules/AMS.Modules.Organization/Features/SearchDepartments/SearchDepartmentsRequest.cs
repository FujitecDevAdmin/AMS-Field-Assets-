namespace AMS.Modules.Organization.Features.SearchDepartments;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchDepartmentsRequest(
    bool? IsActive,
    string? Search);
