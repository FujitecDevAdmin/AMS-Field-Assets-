namespace AMS.Modules.Assets.Features.SearchChartOfAccounts;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchChartOfAccountsRequest(
    bool? IsActive);
