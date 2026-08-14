namespace AMS.Modules.Contracts.Features.SearchContracts;

/// <summary>
/// One page of contracts, soonest to expire first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Contracts matching the filter.</param>
/// <param name="ExpiringCount">How many run out within thirty days. The number the screen puts in red.</param>
public sealed record SearchContractsResponse(
    IReadOnlyList<SearchContractsResponse.Row> Rows,
    int TotalCount,
    int ExpiringCount)
{
    /// <summary>One contract.</summary>
    /// <param name="Id">The contract.</param>
    /// <param name="ContractNumber">What it is quoted by.</param>
    /// <param name="ContractName">What it is called.</param>
    /// <param name="ContractType">Amc, Warranty, Lease, Licence, Service or Insurance.</param>
    /// <param name="VendorId">Organization.Vendor, id only.</param>
    /// <param name="VendorName">Resolved for display.</param>
    /// <param name="StartDate">When cover began.</param>
    /// <param name="EndDate">When it runs out.</param>
    /// <param name="DaysToExpiry">Negative once it has. What the screen colours.</param>
    /// <param name="ContractValue">What it costs.</param>
    /// <param name="AutoRenew">Whether it rolls over rather than lapsing.</param>
    /// <param name="AssetCount">How many assets it covers.</param>
    public sealed record Row(
        int Id,
        string ContractNumber,
        string ContractName,
        string ContractType,
        int? VendorId,
        string? VendorName,
        DateOnly StartDate,
        DateOnly EndDate,
        int DaysToExpiry,
        decimal? ContractValue,
        bool AutoRenew,
        int AssetCount);
}
