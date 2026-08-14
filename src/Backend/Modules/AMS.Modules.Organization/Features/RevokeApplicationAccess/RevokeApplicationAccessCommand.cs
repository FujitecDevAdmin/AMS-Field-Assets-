using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.RevokeApplicationAccess;

/// <summary>
/// Withdraw an employee's access to an application. Catalogue: Grant and revoke application access.
/// </summary>
public sealed record RevokeApplicationAccessCommand(
    int EmployeeId,
    int ApplicationId) : ICommand<RevokeApplicationAccessResponse>;
