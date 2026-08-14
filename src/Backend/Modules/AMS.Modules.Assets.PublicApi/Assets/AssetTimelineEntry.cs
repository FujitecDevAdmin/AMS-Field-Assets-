namespace AMS.Modules.Assets.PublicApi;

/// <summary>
/// One entry on an asset's timeline, as another module describes it.
/// </summary>
/// <remarks>
/// A DTO, not the <c>AssetEvent</c> entity: no entity ever crosses a module
/// boundary (docs/03 §6).
///
/// The name snapshots are deliberate and are the caller's job to fill. An event
/// must still read correctly after the employee leaves or the branch is
/// renamed, and a timeline that resolves names at read time stops being a
/// record of what happened and becomes a record of what is true now.
/// </remarks>
/// <param name="AssetId">The asset this happened to.</param>
/// <param name="EventType">
/// A short kind, spelled from the writing module's smart enum — "Allocated",
/// "HandedOver", "Despatched", "Verified".
/// </param>
/// <param name="Description">What happened, in a sentence somebody can read.</param>
/// <param name="EventOnUtc">When. UTC, from <c>IClock</c>, never the wall clock.</param>
/// <param name="PerformedBy">The username that did it.</param>
/// <param name="EmployeeId">The employee involved, if any. Id only.</param>
/// <param name="EmployeeNameSnapshot">Their name AS IT WAS. Survives them leaving.</param>
/// <param name="LocationId">The branch involved, if any. Id only.</param>
/// <param name="LocationNameSnapshot">Its name AS IT WAS. Survives a rename.</param>
/// <param name="AllocationId">The allocation this came from, if any.</param>
/// <param name="MovementId">The shipment this came from, if any.</param>
/// <param name="ServiceRequestId">The ticket this came from, if any.</param>
/// <param name="ContractId">The contract this came from, if any.</param>
/// <param name="HandoverId">The branch-store handover this came from, if any.</param>
/// <param name="VerificationId">The physical verification this came from, if any.</param>
public sealed record AssetTimelineEntry(
    int AssetId,
    string EventType,
    string Description,
    DateTime EventOnUtc,
    string PerformedBy,
    int? EmployeeId = null,
    string? EmployeeNameSnapshot = null,
    int? LocationId = null,
    string? LocationNameSnapshot = null,
    int? AllocationId = null,
    int? MovementId = null,
    int? ServiceRequestId = null,
    int? ContractId = null,
    int? HandoverId = null,
    int? VerificationId = null);
