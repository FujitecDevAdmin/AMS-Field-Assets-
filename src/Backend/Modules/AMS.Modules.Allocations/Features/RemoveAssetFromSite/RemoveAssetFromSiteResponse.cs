namespace AMS.Modules.Allocations.Features.RemoveAssetFromSite;

/// <summary>
/// The closed mapping. The row stays — history points at it.
/// </summary>
/// <param name="Id">The mapping.</param>
/// <param name="RemovedOnUtc">When it came off site.</param>
public sealed record RemoveAssetFromSiteResponse(
    int Id,
    DateTime RemovedOnUtc);
