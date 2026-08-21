namespace AMS.Modules.Verification.Features.SearchAuditBranches;

/// <summary>Active Branch Master records available to an audit manager.</summary>
/// <param name="Rows">Selectable branches.</param>
public sealed record SearchAuditBranchesResponse(
    IReadOnlyList<SearchAuditBranchesResponse.Row> Rows)
{
    /// <summary>One active Branch Master record.</summary>
    /// <param name="Id">Branch id.</param>
    /// <param name="BranchCode">Stable business code.</param>
    /// <param name="BranchName">Display name.</param>
    public sealed record Row(int Id, string BranchCode, string BranchName);
}
