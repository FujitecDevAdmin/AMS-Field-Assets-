using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.SearchHandovers;

/// <summary>
/// What the branch store is holding. Catalogue screen: Branch Handover.
/// </summary>
public sealed record SearchHandoversQuery(
    string? Status,
    int? BranchLocationId,
    int Skip,
    int Take) : IQuery<SearchHandoversResponse>;
