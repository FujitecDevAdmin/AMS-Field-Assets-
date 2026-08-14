namespace AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateSlaPolicyMapper
{
    public static CreateSlaPolicyCommand ToCommand(CreateSlaPolicyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateSlaPolicyCommand(
            request.PolicyName.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.Priority.Trim(),
            request.ResponseTargetMinutes,
            request.ResolutionTargetMinutes,
            request.RespectOperationalHours ?? true,
            request.RespectHolidays ?? true,
            request.RespectWeekends ?? true,
            request.NearDueWarningMinutes ?? 30);
    }
}
