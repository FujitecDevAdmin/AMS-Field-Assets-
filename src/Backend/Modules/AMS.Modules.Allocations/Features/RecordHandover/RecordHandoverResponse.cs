namespace AMS.Modules.Allocations.Features.RecordHandover;

/// <summary>
/// The handover record.
/// </summary>
/// <param name="Id">The handover.</param>
/// <param name="Status">HandedOver — it is in the branch store, not yet despatched.</param>
/// <param name="ImageCount">Photographs kept as evidence of the state it came back in.</param>
public sealed record RecordHandoverResponse(
    int Id,
    string Status,
    int ImageCount);
