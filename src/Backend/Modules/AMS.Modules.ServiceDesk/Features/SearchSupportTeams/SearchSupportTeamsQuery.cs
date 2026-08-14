using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchSupportTeams;

/// <summary>
/// Teams, their members and their leads. Catalogue screen: Support Teams.
/// </summary>
public sealed record SearchSupportTeamsQuery(
    bool? IsActive,
    int? RegionId) : IQuery<SearchSupportTeamsResponse>;
