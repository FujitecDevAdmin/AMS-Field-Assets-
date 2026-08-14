namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[SupportTeamMember]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SupportTeamMember
{
    public int SupportTeamId { get; set; }

    public int UserId { get; set; }

    public bool IsLead { get; set; }

    public DateTime AddedOnUtc { get; set; }

    public int? AddedByUserId { get; set; }
}
