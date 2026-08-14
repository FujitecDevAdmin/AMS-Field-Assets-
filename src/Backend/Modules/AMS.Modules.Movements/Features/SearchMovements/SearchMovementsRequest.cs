namespace AMS.Modules.Movements.Features.SearchMovements;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchMovementsRequest(
    string? Status,
    int? AssetId,
    int? FromLocationId,
    int? ToLocationId,
    int? MovementBatchId,
    int? Skip,
    int? Take);
