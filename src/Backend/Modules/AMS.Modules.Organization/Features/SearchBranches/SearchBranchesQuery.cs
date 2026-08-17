using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchBranches;

/// <summary>
/// The branch list. Catalogue screen: Branches.
/// </summary>
public sealed record SearchBranchesQuery(
    bool? IsActive,
    int? RegionId,
    string? Search) : IQuery<SearchBranchesResponse>;
