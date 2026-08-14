using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Movements.Features.ReceiveMovement;

/// <summary>
/// Confirm arrival at the destination. Catalogue: Receive at the destination, and Goods receipt at head office.
/// </summary>
public sealed record ReceiveMovementCommand(
    int Id,
    string? ReceiptRemarks) : ICommand<ReceiveMovementResponse>;
