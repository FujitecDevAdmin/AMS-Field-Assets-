namespace AMS.Modules.Organization.Features.GetEmployee;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetEmployeeRequest(
    int EmployeeId);
