namespace AMS.Modules.Organization.Features.GetMyApplicationAccess;

/// <summary>The caller's own application access.</summary>
/// <param name="EmployeeId">
/// Null when this login has no employee record — a service account, or an
/// administrator who is not in the directory.
/// </param>
/// <param name="Rows">
/// Current grants only. An employee has no reason to see what was withdrawn
/// from them.
/// </param>
public sealed record GetMyApplicationAccessResponse(
    int? EmployeeId,
    IReadOnlyList<GetMyApplicationAccessResponse.Row> Rows)
{
    /// <summary>One application the caller may use.</summary>
    /// <param name="ApplicationId">The application.</param>
    /// <param name="ApplicationName">For display.</param>
    /// <param name="ApplicationLoginId">Their username there, if recorded.</param>
    /// <param name="GrantedOnUtc">When access was given.</param>
    public sealed record Row(
        int ApplicationId,
        string ApplicationName,
        string? ApplicationLoginId,
        DateTime GrantedOnUtc);
}
