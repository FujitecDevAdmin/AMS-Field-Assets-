using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.GetUser;

/// <summary>
/// One user, with the roles and branches the screen edits.
/// </summary>
public sealed record GetUserQuery(
    int UserId) : IQuery<GetUserResponse>;
