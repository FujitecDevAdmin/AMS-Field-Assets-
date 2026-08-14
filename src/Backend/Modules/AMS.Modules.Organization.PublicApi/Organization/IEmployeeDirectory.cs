namespace AMS.Modules.Organization.PublicApi.Organization;

/// <summary>Who reports to whom, and who works where.</summary>
/// <remarks>
/// The reporting line is Organization's to know. Every other module that needs
/// "this person's manager" — approval routing, SLA escalation, the joiner
/// workflow — asks here rather than reading Organization.Employee, because a
/// second reader of that column is a second place the rule about acting
/// managers has to be repeated.
/// </remarks>
public interface IEmployeeDirectory
{
    /// <summary>
    /// The employee this one reports to, or null: no manager recorded, or the
    /// employee itself is unknown.
    /// </summary>
    Task<int?> ManagerOfAsync(int employeeId, CancellationToken ct);

    /// <summary>The branch an employee belongs to, or null if unknown.</summary>
    Task<int?> BranchOfAsync(int employeeId, CancellationToken ct);
}
