namespace AMS.Modules.Allocations.Features.SearchAllocations;

/// <summary>
/// One page of allocations.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Allocations matching the filter.</param>
public sealed record SearchAllocationsResponse(
    IReadOnlyList<SearchAllocationsResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One allocation.</summary>
    /// <param name="Id">The allocation.</param>
    /// <param name="AssetId">The asset. Id only.</param>
    /// <param name="EmployeeId">Who is accountable.</param>
    /// <param name="LocationId">The branch.</param>
    /// <param name="AllocatedOnUtc">When it was issued.</param>
    /// <param name="ExpectedReturnDate">For a temporary issue. Null means indefinite.</param>
    /// <param name="ReturnRequestedOnUtc">When the employee asked to give it back.</param>
    /// <param name="ReturnedOnUtc">When it came back. Null while the allocation is live.</param>
    /// <param name="IsOverdue">
    /// Past its expected return date and not yet returned. Computed on read
    /// rather than stored: a stored flag is wrong every night until a job fixes it.
    /// </param>
    /// <param name="AcknowledgementStatus">Pending, Signed or Approved. Null before one exists.</param>
    public sealed record Row(
        int Id,
        int AssetId,
        int EmployeeId,
        int? LocationId,
        DateTime AllocatedOnUtc,
        DateOnly? ExpectedReturnDate,
        DateTime? ReturnRequestedOnUtc,
        DateTime? ReturnedOnUtc,
        bool IsOverdue,
        string? AcknowledgementStatus);
}
