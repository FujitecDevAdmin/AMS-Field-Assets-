namespace AMS.Modules.Allocations.Features.SearchHandovers;

/// <summary>
/// One page of handovers.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Handovers matching the filter.</param>
public sealed record SearchHandoversResponse(
    IReadOnlyList<SearchHandoversResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One asset sitting in a branch store.</summary>
    /// <param name="Id">The handover.</param>
    /// <param name="AllocationId">The allocation it closed.</param>
    /// <param name="AssetId">The asset. Id only.</param>
    /// <param name="FromEmployeeId">Who gave it back.</param>
    /// <param name="BranchLocationId">Which store is holding it.</param>
    /// <param name="Status">HandedOver, InTransitToHo, ReceivedAtHo or Cancelled.</param>
    /// <param name="ReturnCondition">Good, MinorDamage, Damaged, NotWorking or Missing.</param>
    /// <param name="Remarks">Mandatory. What state it came back in, in words.</param>
    /// <param name="HandedOverOnUtc">When the branch took it.</param>
    /// <param name="ImageCount">Photographs kept as evidence.</param>
    public sealed record Row(
        int Id,
        int AllocationId,
        int AssetId,
        int FromEmployeeId,
        int BranchLocationId,
        string Status,
        string ReturnCondition,
        string Remarks,
        DateTime HandedOverOnUtc,
        int ImageCount);
}
