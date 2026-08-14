namespace AMS.Modules.Assets.Features.SearchChartOfAccounts;

/// <summary>
/// Every code with its description.
/// </summary>
/// <param name="Rows">The codes, in code order.</param>
public sealed record SearchChartOfAccountsResponse(
    IReadOnlyList<SearchChartOfAccountsResponse.Row> Rows)
{
    /// <summary>One chart-of-account code.</summary>
    /// <param name="Id">The code.</param>
    /// <param name="CoaCode">Unique, enforced by UX_ChartOfAccount_Code.</param>
    /// <param name="Description">
    /// Stored once here rather than inline on every asset, so correcting a typo
    /// in the ledger does not leave seven thousand stale copies behind.
    /// </param>
    /// <param name="IsActive">Retiring hides it from pickers; finance records keep pointing here.</param>
    public sealed record Row(int Id, string CoaCode, string? Description, bool IsActive);
}
