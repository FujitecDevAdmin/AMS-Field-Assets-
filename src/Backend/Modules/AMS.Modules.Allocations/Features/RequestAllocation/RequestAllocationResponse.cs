namespace AMS.Modules.Allocations.Features.RequestAllocation;

/// <summary>
/// The new request, Pending.
/// </summary>
/// <param name="Id">The request.</param>
/// <param name="Status">Always Pending. Somebody else decides it — that is the point.</param>
public sealed record RequestAllocationResponse(
    int Id,
    string Status);
