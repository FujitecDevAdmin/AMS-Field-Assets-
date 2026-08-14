namespace AMS.Modules.Assets.Features.GetAssetTimeline;

/// <summary>
/// One page of the timeline, and how many entries there are.
/// </summary>
/// <param name="Rows">The page, newest first.</param>
/// <param name="TotalCount">Entries against this asset, ignoring paging.</param>
public sealed record GetAssetTimelineResponse(
    IReadOnlyList<GetAssetTimelineResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One thing that happened.</summary>
    /// <param name="Id">The entry.</param>
    /// <param name="EventType">Registered, Allocated, Despatched, Verified, and so on.</param>
    /// <param name="Description">What happened, in a sentence somebody can read.</param>
    /// <param name="EventOnUtc">When.</param>
    /// <param name="PerformedBy">The username that did it.</param>
    /// <param name="EmployeeId">The employee involved, if any.</param>
    /// <param name="EmployeeNameSnapshot">
    /// Their name AS IT WAS. Deliberately a snapshot: an event must still read
    /// correctly after they leave, and a timeline that resolves names at read
    /// time stops being a record of what happened and becomes a record of what
    /// is true now.
    /// </param>
    /// <param name="LocationId">The branch involved, if any.</param>
    /// <param name="LocationNameSnapshot">Its name AS IT WAS. Survives a rename.</param>
    /// <param name="QuantityDelta">
    /// Signed, on bulk lines: a receipt of 200 is +200, an issue of 5 is −5. Null
    /// on a unit asset's events. This is what lets the timeline reconcile
    /// against the holdings instead of merely narrating them.
    /// </param>
    /// <param name="AllocationId">The allocation this came from, if any.</param>
    /// <param name="MovementId">The shipment this came from, if any.</param>
    /// <param name="ServiceRequestId">The ticket this came from, if any.</param>
    /// <param name="ContractId">The contract this came from, if any.</param>
    /// <param name="HandoverId">The branch-store handover this came from, if any.</param>
    /// <param name="VerificationId">The physical verification this came from, if any.</param>
    /// <param name="DisposalId">The disposal this came from, if any.</param>
    public sealed record Row(
        long Id,
        string EventType,
        string Description,
        DateTime EventOnUtc,
        string PerformedBy,
        int? EmployeeId,
        string? EmployeeNameSnapshot,
        int? LocationId,
        string? LocationNameSnapshot,
        decimal? QuantityDelta,
        int? AllocationId,
        int? MovementId,
        int? ServiceRequestId,
        int? ContractId,
        int? HandoverId,
        int? VerificationId,
        int? DisposalId);
}
