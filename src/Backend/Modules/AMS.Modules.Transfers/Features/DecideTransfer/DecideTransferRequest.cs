namespace AMS.Modules.Transfers.Features.DecideTransfer;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DecideTransferRequest(
    bool Approved,
    string? Remarks);
