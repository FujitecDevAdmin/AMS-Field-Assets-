namespace AMS.Modules.Organization.Features.CreateEmployee;

/// <summary>
/// The new employee.
/// </summary>
/// <param name="Id">The new employee.</param>
/// <param name="EmployeeCode">Unique, upper-cased.</param>
/// <param name="FullName">As stored, trimmed.</param>
/// <param name="ETag">The ConcurrencyStamp (R2-22).</param>
public sealed record CreateEmployeeResponse(
    int Id,
    string EmployeeCode,
    string FullName,
    string ETag);
