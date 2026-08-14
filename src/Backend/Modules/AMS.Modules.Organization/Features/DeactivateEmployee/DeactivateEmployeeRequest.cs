namespace AMS.Modules.Organization.Features.DeactivateEmployee;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DeactivateEmployeeRequest(
    string ETag);
