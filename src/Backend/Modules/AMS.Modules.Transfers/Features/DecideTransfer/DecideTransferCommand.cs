using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Transfers.Features.DecideTransfer;

/// <summary>
/// Approve or reject a transfer. Catalogue: with a remark.
/// </summary>
public sealed record DecideTransferCommand(
    int Id,
    bool Approved,
    string? Remarks) : ICommand<DecideTransferResponse>;
