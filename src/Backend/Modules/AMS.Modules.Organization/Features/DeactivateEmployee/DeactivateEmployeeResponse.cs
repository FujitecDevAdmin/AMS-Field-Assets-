namespace AMS.Modules.Organization.Features.DeactivateEmployee;

/// <summary>
/// The deactivated employee.
/// </summary>
/// <param name="Id">The leaver.</param>
/// <param name="IsActive">False. The row stays: assets, tickets and history point at it.</param>
/// <param name="DirectReportsReassigned">How many people reported to this employee and now report to nobody. The caller must give them a new manager; leaving them pointing at a leaver is worse.</param>
public sealed record DeactivateEmployeeResponse(
    int Id,
    bool IsActive,
    int DirectReportsReassigned);
