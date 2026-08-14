using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Transfers.Features.CancelTransfer;

/// <summary>
/// Withdraw a transfer before it is completed. Catalogue: Cancel.
/// </summary>
public sealed record CancelTransferCommand(
    int Id,
    string Reason) : ICommand<CancelTransferResponse>;
