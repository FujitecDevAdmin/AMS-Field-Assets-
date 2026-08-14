namespace AMS.Modules.Allocations.Features.ReceiveReturn;

/// <summary>
/// The closed allocation.
/// </summary>
/// <param name="Id">The allocation.</param>
/// <param name="AssetId">The asset, now free to issue again.</param>
public sealed record ReceiveReturnResponse(
    int Id,
    int AssetId);
