namespace AMS.Modules.Organization.Features.UpdateEmployee;

/// <summary>
/// The updated employee.
/// </summary>
/// <param name="Id">The employee edited.</param>
/// <param name="FullName">As stored, trimmed.</param>
/// <param name="ETag">The NEW ConcurrencyStamp. The client must send this one next.</param>
public sealed record UpdateEmployeeResponse(
    int Id,
    string FullName,
    string ETag);
