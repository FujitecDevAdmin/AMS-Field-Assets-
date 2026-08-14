using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.SearchRoles;

/// <summary>
/// The role list with its capability counts.
/// </summary>
public sealed record SearchRolesQuery(
    bool? IsActive) : IQuery<SearchRolesResponse>;
