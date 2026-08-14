using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchRegions;

/// <summary>
/// The region list. Catalogue screen: Regions.
/// </summary>
public sealed record SearchRegionsQuery(
    bool? IsActive,
    string? Search) : IQuery<SearchRegionsResponse>;
