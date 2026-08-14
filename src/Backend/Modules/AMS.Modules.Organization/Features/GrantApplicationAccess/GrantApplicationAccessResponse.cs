namespace AMS.Modules.Organization.Features.GrantApplicationAccess;

/// <summary>
/// The grant.
/// </summary>
/// <param name="Id">The grant row.</param>
/// <param name="EmployeeId">Who may now use it.</param>
/// <param name="ApplicationId">What they may use.</param>
/// <param name="GrantedOnUtc">When. UTC, like every instant.</param>
public sealed record GrantApplicationAccessResponse(
    int Id,
    int EmployeeId,
    int ApplicationId,
    DateTime GrantedOnUtc);
