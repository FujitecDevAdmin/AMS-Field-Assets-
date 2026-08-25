namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestCategory]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestCategory
{
    public int Id { get; set; }

    public required string CategoryName { get; set; }

    public required string CategoryType { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
