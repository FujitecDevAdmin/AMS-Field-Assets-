namespace AMS.Modules.Transfers.Features.CompleteTransfer;

/// <summary>
/// The completed transfer.
/// </summary>
/// <param name="Id">The request.</param>
/// <param name="Status">Completed. The register now says what the transfer asked for.</param>
/// <param name="SapSyncStatus">Pending when SAP needs telling, NotRequired when it does not.</param>
public sealed record CompleteTransferResponse(
    int Id,
    string Status,
    string SapSyncStatus);
