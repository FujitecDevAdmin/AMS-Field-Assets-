namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestSubCategory]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestSubCategory
{
    public int Id { get; set; }

    public int RequestCategoryId { get; set; }

    public required string SubCategoryName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
