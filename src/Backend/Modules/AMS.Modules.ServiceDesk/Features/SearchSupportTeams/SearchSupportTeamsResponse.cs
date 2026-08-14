namespace AMS.Modules.ServiceDesk.Features.SearchSupportTeams;

/// <summary>
/// Every team matching the filter.
/// </summary>
/// <param name="Rows">The teams, alphabetically.</param>
public sealed record SearchSupportTeamsResponse(
    IReadOnlyList<SearchSupportTeamsResponse.Row> Rows)
{
    /// <summary>One support team.</summary>
    /// <param name="Id">The team.</param>
    /// <param name="TeamName">Unique, enforced by UX_SupportTeam_Name.</param>
    /// <param name="RegionId">Which region it serves. Id only — Organization is another module.</param>
    /// <param name="MailboxAddress">Where its e-mail goes, if it has one.</param>
    /// <param name="IsDefaultTeam">
    /// The fallback when routing finds nothing. Exactly one team may carry it,
    /// enforced by UX_SupportTeam_OneDefault.
    /// </param>
    /// <param name="IsActive">Retired teams stay: tickets are still assigned to them.</param>
    /// <param name="MemberCount">People in it.</param>
    /// <param name="LeadUserIds">Who leads it. Usually one, occasionally two.</param>
    public sealed record Row(
        int Id,
        string TeamName,
        int? RegionId,
        string? MailboxAddress,
        bool IsDefaultTeam,
        bool IsActive,
        int MemberCount,
        IReadOnlyList<int> LeadUserIds);
}
