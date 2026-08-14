namespace AMS.Modules.ServiceDesk.Features.SetSupportTeamMembers;

/// <summary>
/// The team's membership as it now stands.
/// </summary>
/// <param name="SupportTeamId">The team.</param>
/// <param name="MemberCount">How many people are in it.</param>
/// <param name="LeadCount">How many of them lead it.</param>
public sealed record SetSupportTeamMembersResponse(
    int SupportTeamId,
    int MemberCount,
    int LeadCount);
