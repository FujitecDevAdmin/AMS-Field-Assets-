namespace AMS.Modules.Transfers.Features.CancelTransfer;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CancelTransferRequest(
    string Reason);
