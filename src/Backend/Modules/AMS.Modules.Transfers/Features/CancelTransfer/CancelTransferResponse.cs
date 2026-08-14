namespace AMS.Modules.Transfers.Features.CancelTransfer;

/// <summary>
/// The cancelled request. The row stays — it is the record it was asked for.
/// </summary>
/// <param name="Id">The request.</param>
/// <param name="Status">Cancelled.</param>
public sealed record CancelTransferResponse(
    int Id,
    string Status);
