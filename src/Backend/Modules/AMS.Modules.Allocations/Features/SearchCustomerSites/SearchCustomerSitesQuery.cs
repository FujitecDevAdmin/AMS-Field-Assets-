using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.SearchCustomerSites;

/// <summary>
/// The site master. Catalogue screen: Customer Sites.
/// </summary>
public sealed record SearchCustomerSitesQuery(
    string? Search,
    bool? IsActive) : IQuery<SearchCustomerSitesResponse>;
