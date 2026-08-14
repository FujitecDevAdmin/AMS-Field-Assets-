namespace AMS.Modules.Organization.Features.SearchDepartments;

/// <summary>
/// Every department matching the filter. Not paged: these tables hold tens of
/// rows.
/// </summary>
/// <param name="Rows">The departments.</param>
public sealed record SearchDepartmentsResponse(IReadOnlyList<SearchDepartmentsResponse.Row> Rows)
{
    /// <summary>One department.</summary>
    /// <param name="Id">The department.</param>
    /// <param name="DepartmentName">Unique, enforced by UX_Department_Name.</param>
    /// <param name="IsActive">Retired departments stay, because employees still point at them.</param>
    /// <param name="EmployeeCount">Employees in it.</param>
    public sealed record Row(int Id, string DepartmentName, bool IsActive, int EmployeeCount);
}
