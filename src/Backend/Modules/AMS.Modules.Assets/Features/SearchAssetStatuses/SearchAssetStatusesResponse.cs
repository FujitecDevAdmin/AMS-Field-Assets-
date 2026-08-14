namespace AMS.Modules.Assets.Features.SearchAssetStatuses;

/// <summary>
/// Every status, in display order.
/// </summary>
/// <param name="Rows">The statuses.</param>
public sealed record SearchAssetStatusesResponse(
    IReadOnlyList<SearchAssetStatusesResponse.Row> Rows)
{
    /// <summary>One asset status.</summary>
    /// <param name="Id">The status.</param>
    /// <param name="StatusName">Unique, enforced by UX_AssetStatus_Name.</param>
    /// <param name="IsTerminal">
    /// A terminal status ends the asset's working life. Scrapped, Lost and
    /// Disposed are the seeded ones; an asset in a terminal status cannot be
    /// allocated again.
    /// </param>
    /// <param name="DisplayOrder">The order the picker shows them in. Gaps are deliberate.</param>
    /// <param name="IsActive">Retired statuses stay, because assets still sit in them.</param>
    /// <param name="AssetCount">Assets currently in this status, excluding deleted ones.</param>
    public sealed record Row(
        int Id,
        string StatusName,
        bool IsTerminal,
        int DisplayOrder,
        bool IsActive,
        int AssetCount);
}
