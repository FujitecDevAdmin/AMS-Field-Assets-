using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.GrantApplicationAccess;

/// <summary>
/// Record that an employee may use an application. Catalogue: Grant and revoke application access.
/// </summary>
public sealed record GrantApplicationAccessCommand(
    int EmployeeId,
    int ApplicationId,
    string? ApplicationLoginId) : ICommand<GrantApplicationAccessResponse>;
