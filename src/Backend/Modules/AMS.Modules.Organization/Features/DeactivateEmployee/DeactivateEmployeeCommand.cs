using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.DeactivateEmployee;

/// <summary>
/// Mark a leaver inactive. Catalogue: Deactivate a leaver.
/// </summary>
public sealed record DeactivateEmployeeCommand(
    int EmployeeId,
    string ETag) : ICommand<DeactivateEmployeeResponse>;
