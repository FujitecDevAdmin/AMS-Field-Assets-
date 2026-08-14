using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Movements.Features.SearchMovements;

/// <summary>
/// Shipments and where they have got to. Catalogue screen: Despatch.
/// </summary>
public sealed record SearchMovementsQuery(
    string? Status,
    int? AssetId,
    int? FromLocationId,
    int? ToLocationId,
    int? MovementBatchId,
    int Skip,
    int Take) : IQuery<SearchMovementsResponse>;
