namespace AMS.Modules.Allocations.Features.RequestAllocation;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RequestAllocationMapper
{
    public static RequestAllocationCommand ToCommand(RequestAllocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RequestAllocationCommand(
            request.AssetId,
            request.EmployeeId,
            request.LocationId,
            string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim());
    }
}
