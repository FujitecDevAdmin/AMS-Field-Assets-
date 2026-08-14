namespace AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateSlaPolicyRequest(
    string PolicyName,
    string? Description,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes,
    bool? RespectOperationalHours,
    bool? RespectHolidays,
    bool? RespectWeekends,
    int? NearDueWarningMinutes,
    bool? IsActive);
