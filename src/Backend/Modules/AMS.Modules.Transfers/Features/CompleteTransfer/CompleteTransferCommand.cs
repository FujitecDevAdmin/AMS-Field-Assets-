using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Transfers.Features.CompleteTransfer;

/// <summary>
/// Apply an approved transfer. Catalogue: applies the change and queues it to SAP where the accounting system needs to know.
/// </summary>
public sealed record CompleteTransferCommand(
    int Id,
    int? MovementId) : ICommand<CompleteTransferResponse>;
