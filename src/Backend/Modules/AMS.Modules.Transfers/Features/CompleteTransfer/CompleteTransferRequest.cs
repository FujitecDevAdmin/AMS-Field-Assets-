namespace AMS.Modules.Transfers.Features.CompleteTransfer;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CompleteTransferRequest(
    int? MovementId);
