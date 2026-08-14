namespace AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ChangeRequestStatusMapper
{
    public static ChangeRequestStatusCommand ToCommand(ChangeRequestStatusRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ChangeRequestStatusCommand(
            id,
            request.RequestStatusId,
            string.IsNullOrWhiteSpace(request.Resolution) ? null : request.Resolution.Trim(),
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
