namespace AMS.Modules.Transfers.Features.RaiseTransfer;

/// <summary>
/// The new request, Pending.
/// </summary>
/// <param name="Id">The request.</param>
/// <param name="TransferType">Employee, Department, Branch or CostCenter.</param>
/// <param name="Status">Always Pending. Somebody else decides it.</param>
public sealed record RaiseTransferResponse(
    int Id,
    string TransferType,
    string Status);
