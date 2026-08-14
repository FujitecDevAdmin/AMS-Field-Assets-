using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SetSupportTeamMembers;

/// <summary>
/// Set who is in a team and who leads it. Catalogue: Teams, members and leads.
/// </summary>
public sealed record SetSupportTeamMembersCommand(
    int SupportTeamId,
    IReadOnlyList<SetSupportTeamMembersCommand.Member> Members) : ICommand<SetSupportTeamMembersResponse>
{
    /// <summary>One person in the team.</summary>
    /// <param name="UserId">Identity.User, id only.</param>
    /// <param name="IsLead">
    /// Whether they lead it. A team with members needs at least one, because
    /// escalation has to reach somebody by name.
    /// </param>
    public sealed record Member(int UserId, bool IsLead);
}
