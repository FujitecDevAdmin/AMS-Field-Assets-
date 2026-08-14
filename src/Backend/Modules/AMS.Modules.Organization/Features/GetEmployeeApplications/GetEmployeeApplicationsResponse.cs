namespace AMS.Modules.Organization.Features.GetEmployeeApplications;

/// <summary>One employee's application access.</summary>
/// <param name="EmployeeId">The employee asked about.</param>
/// <param name="Rows">Their grants, current and optionally withdrawn.</param>
public sealed record GetEmployeeApplicationsResponse(
    int EmployeeId,
    IReadOnlyList<GetEmployeeApplicationsResponse.Row> Rows)
{
    /// <summary>One grant.</summary>
    /// <param name="Id">The grant row.</param>
    /// <param name="ApplicationId">What was granted.</param>
    /// <param name="ApplicationName">For display.</param>
    /// <param name="ApplicationLoginId">Their username in that application, if recorded.</param>
    /// <param name="GrantedOnUtc">When access was given.</param>
    /// <param name="RevokedOnUtc">
    /// When it was withdrawn, or null while it is still held. The row is never
    /// deleted: it is the record that access WAS held, which is exactly what an
    /// audit asks about after somebody leaves.
    /// </param>
    public sealed record Row(
        int Id,
        int ApplicationId,
        string ApplicationName,
        string? ApplicationLoginId,
        DateTime GrantedOnUtc,
        DateTime? RevokedOnUtc);
}
