namespace AMS.Modules.ServiceDesk.Features.AssignServiceRequest;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class AssignServiceRequestMapper
{
    public static AssignServiceRequestCommand ToCommand(AssignServiceRequestRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AssignServiceRequestCommand(
            id,
            request.AssignedToUserId,
            request.AssignedTeamId,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
