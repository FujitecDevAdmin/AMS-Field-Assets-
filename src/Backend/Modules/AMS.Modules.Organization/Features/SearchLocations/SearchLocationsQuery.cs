using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchLocations;

/// <summary>
/// The branch list. Catalogue screen: Branches.
/// </summary>
public sealed record SearchLocationsQuery(
    bool? IsActive,
    int? RegionId,
    string? Search) : IQuery<SearchLocationsResponse>;
