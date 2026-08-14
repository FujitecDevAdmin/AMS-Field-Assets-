namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AssetSiteMapping]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetSiteMapping
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public int CustomerSiteId { get; set; }

    public DateOnly? CommissionedDate { get; set; }

    public DateTime MappedOnUtc { get; set; }

    public DateTime? RemovedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
