using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

/// <summary>
/// Add an SLA policy. Catalogue: SLA Policy Setup.
/// </summary>
public sealed record CreateSlaPolicyCommand(
    string PolicyName,
    string? Description,
    string Priority,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes,
    bool RespectOperationalHours,
    bool RespectHolidays,
    bool RespectWeekends,
    int NearDueWarningMinutes) : ICommand<CreateSlaPolicyResponse>;
