namespace AMS.Modules.Allocations.Features.ApproveAcknowledgement;

/// <summary>
/// The acknowledgement, now Approved.
/// </summary>
/// <param name="Id">The acknowledgement.</param>
/// <param name="Status">Approved.</param>
public sealed record ApproveAcknowledgementResponse(
    int Id,
    string Status);
