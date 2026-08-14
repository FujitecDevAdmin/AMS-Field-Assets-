namespace AMS.Modules.Movements.Features.SearchMovements;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SearchMovementsMapper
{
    public static SearchMovementsQuery ToQuery(SearchMovementsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SearchMovementsQuery(
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            request.AssetId,
            request.FromLocationId,
            request.ToLocationId,
            request.MovementBatchId,
            request.Skip ?? 0,
            request.Take ?? 50);
    }
}
