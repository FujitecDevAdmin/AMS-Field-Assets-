namespace AMS.Modules.Organization.Features.SearchEmployees;

/// <summary>One page of employees, and how many match in total.</summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Employees matching the filter, ignoring paging.</param>
public sealed record SearchEmployeesResponse(
    IReadOnlyList<SearchEmployeesResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One line of the directory grid.</summary>
    /// <param name="Id">The employee.</param>
    /// <param name="EmployeeCode">Unique, upper-cased.</param>
    /// <param name="FullName">As stored.</param>
    /// <param name="Email">May be null.</param>
    /// <param name="Phone">May be null.</param>
    /// <param name="DepartmentName">Null when the employee has no department.</param>
    /// <param name="BranchName">Null when the employee has no branch.</param>
    /// <param name="ReportingManagerName">Null when they report to nobody.</param>
    /// <param name="IsActive">Leavers stay in the directory, greyed.</param>
    public sealed record Row(
        int Id,
        string EmployeeCode,
        string FullName,
        string? Email,
        string? Phone,
        string? DepartmentName,
        string? BranchName,
        string? ReportingManagerName,
        bool IsActive);
}
