namespace AMS.Modules.ServiceDesk.Features.AddRequestNote;

/// <summary>
/// The entry, as it now sits in the timeline.
/// </summary>
/// <param name="Id">The history entry.</param>
/// <param name="ServiceRequestId">The ticket.</param>
/// <param name="IsInternal">Hidden from the requester. Never hidden from audit.</param>
/// <param name="OccurredOnUtc">When it was written.</param>
public sealed record AddRequestNoteResponse(
    long Id,
    int ServiceRequestId,
    bool IsInternal,
    DateTime OccurredOnUtc);
