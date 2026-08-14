namespace AMS.Modules.Allocations.Features.SignAcknowledgement;

/// <summary>
/// The acknowledgement, now Signed.
/// </summary>
/// <param name="Id">The acknowledgement.</param>
/// <param name="Status">Signed, awaiting the manager.</param>
public sealed record SignAcknowledgementResponse(
    int Id,
    string Status);
