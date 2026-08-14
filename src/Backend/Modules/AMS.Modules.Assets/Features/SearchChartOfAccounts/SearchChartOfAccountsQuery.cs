using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SearchChartOfAccounts;

/// <summary>
/// The ledger codes an asset's finance record points at.
/// </summary>
public sealed record SearchChartOfAccountsQuery(
    bool? IsActive) : IQuery<SearchChartOfAccountsResponse>;
