using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.SearchUsers;

/// <summary>
/// The Users list, filtered and paged.
/// </summary>
public sealed record SearchUsersQuery(
    string? Search,
    bool? IsActive,
    int Skip,
    int Take) : IQuery<SearchUsersResponse>;
