namespace AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateSlaPolicyMapper
{
    public static UpdateSlaPolicyCommand ToCommand(UpdateSlaPolicyRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateSlaPolicyCommand(
            id,
            request.PolicyName.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.ResponseTargetMinutes,
            request.ResolutionTargetMinutes,
            request.RespectOperationalHours ?? true,
            request.RespectHolidays ?? true,
            request.RespectWeekends ?? true,
            request.NearDueWarningMinutes ?? 30,
            request.IsActive ?? true);
    }
}
