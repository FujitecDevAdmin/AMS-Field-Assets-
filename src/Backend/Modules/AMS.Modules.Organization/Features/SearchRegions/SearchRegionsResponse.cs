namespace AMS.Modules.Organization.Features.SearchRegions;

/// <summary>
/// Every region matching the filter. These tables hold tens of rows, so the
/// list is not paged.
/// </summary>
/// <param name="Rows">The regions.</param>
public sealed record SearchRegionsResponse(IReadOnlyList<SearchRegionsResponse.Row> Rows)
{
    /// <summary>One region.</summary>
    /// <param name="Id">The region.</param>
    /// <param name="RegionName">Unique, enforced by UX_Region_Name.</param>
    /// <param name="Description">May be null.</param>
    /// <param name="IsActive">Retired regions stay, because branches still point at them.</param>
    /// <param name="BranchCount">Branches in this region. Retiring one that has branches is worth a warning.</param>
    public sealed record Row(int Id, string RegionName, string? Description, bool IsActive, int BranchCount);
}
