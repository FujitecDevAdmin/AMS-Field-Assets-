namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetStatus]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetStatus
{
    public int Id { get; set; }

    public required string StatusName { get; set; }

    public bool IsTerminal { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
