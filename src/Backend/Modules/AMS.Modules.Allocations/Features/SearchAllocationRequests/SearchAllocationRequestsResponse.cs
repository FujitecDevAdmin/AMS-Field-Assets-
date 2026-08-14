namespace AMS.Modules.Allocations.Features.SearchAllocationRequests;

/// <summary>
/// One page of requests, newest first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Requests matching the filter.</param>
public sealed record SearchAllocationRequestsResponse(
    IReadOnlyList<SearchAllocationRequestsResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One request in the approval queue.</summary>
    /// <param name="Id">The request.</param>
    /// <param name="AssetId">The asset asked for. Id only — Assets is another module.</param>
    /// <param name="EmployeeId">Who it is for.</param>
    /// <param name="LocationId">The branch it would sit at.</param>
    /// <param name="Status">Pending, Approved or Rejected.</param>
    /// <param name="RequestedByUserId">Who raised it.</param>
    /// <param name="RequestedOnUtc">When.</param>
    /// <param name="DecidedByUserId">Who decided, once somebody has.</param>
    /// <param name="DecidedOnUtc">When they did.</param>
    /// <param name="DecisionRemarks">Why. Stays on the record either way.</param>
    /// <param name="AllocationId">The allocation this produced, if it was acted on.</param>
    public sealed record Row(
        int Id,
        int AssetId,
        int EmployeeId,
        int? LocationId,
        string Status,
        int RequestedByUserId,
        DateTime RequestedOnUtc,
        int? DecidedByUserId,
        DateTime? DecidedOnUtc,
        string? DecisionRemarks,
        int? AllocationId);
}
