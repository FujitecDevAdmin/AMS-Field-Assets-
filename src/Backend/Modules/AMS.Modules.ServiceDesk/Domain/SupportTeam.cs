namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[SupportTeam]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SupportTeam
{
    public int Id { get; set; }

    public required string TeamName { get; set; }

    public int? RegionId { get; set; }

    public string? MailboxAddress { get; set; }

    public bool IsDefaultTeam { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
