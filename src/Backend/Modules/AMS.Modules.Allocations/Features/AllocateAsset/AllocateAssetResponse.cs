namespace AMS.Modules.Allocations.Features.AllocateAsset;

/// <summary>
/// The new allocation.
/// </summary>
/// <param name="Id">The allocation.</param>
/// <param name="AssetId">The asset now issued.</param>
/// <param name="EmployeeId">Who is accountable for it.</param>
public sealed record AllocateAssetResponse(
    int Id,
    int AssetId,
    int EmployeeId);
