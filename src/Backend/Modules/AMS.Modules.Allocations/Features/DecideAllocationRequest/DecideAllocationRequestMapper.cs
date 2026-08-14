namespace AMS.Modules.Allocations.Features.DecideAllocationRequest;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class DecideAllocationRequestMapper
{
    public static DecideAllocationRequestCommand ToCommand(DecideAllocationRequestRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new DecideAllocationRequestCommand(
            id,
            request.Approved,
            string.IsNullOrWhiteSpace(request.DecisionRemarks) ? null : request.DecisionRemarks.Trim());
    }
}
