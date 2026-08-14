using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchVendors;

/// <summary>
/// The vendor list. Catalogue screen: Vendors.
/// </summary>
public sealed record SearchVendorsQuery(
    bool? IsActive,
    string? Search) : IQuery<SearchVendorsResponse>;
