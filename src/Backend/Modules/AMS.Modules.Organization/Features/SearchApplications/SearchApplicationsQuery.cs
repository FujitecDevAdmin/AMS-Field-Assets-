using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchApplications;

/// <summary>
/// The application master. Catalogue: the list of business applications access can be granted to.
/// </summary>
public sealed record SearchApplicationsQuery(
    bool? IsActive,
    string? Search) : IQuery<SearchApplicationsResponse>;
