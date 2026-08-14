namespace AMS.Modules.Transfers.Features.DecideTransfer;

/// <summary>
/// The decided request.
/// </summary>
/// <param name="Id">The request.</param>
/// <param name="Status">Approved or Rejected. Approved does NOT mean applied — completing does that.</param>
public sealed record DecideTransferResponse(
    int Id,
    string Status);
