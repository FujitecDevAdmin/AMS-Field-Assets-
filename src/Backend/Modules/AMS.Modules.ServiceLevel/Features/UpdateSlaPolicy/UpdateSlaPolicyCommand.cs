using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;

/// <summary>
/// Edit an SLA policy or retire it. Catalogue: SLA Policy Setup.
/// </summary>
public sealed record UpdateSlaPolicyCommand(
    int Id,
    string PolicyName,
    string? Description,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes,
    bool RespectOperationalHours,
    bool RespectHolidays,
    bool RespectWeekends,
    int NearDueWarningMinutes,
    bool IsActive) : ICommand<UpdateSlaPolicyResponse>;
