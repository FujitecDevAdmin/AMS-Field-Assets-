namespace AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateSlaPolicyRequest(
    string PolicyName,
    string? Description,
    string Priority,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes,
    bool? RespectOperationalHours,
    bool? RespectHolidays,
    bool? RespectWeekends,
    int? NearDueWarningMinutes);
