namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetClass]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetClass
{
    public int Id { get; set; }

    public required string ClassCode { get; set; }

    public required string ClassName { get; set; }

    public required string ReportingCategory { get; set; }

    public bool IsDepreciable { get; set; }

    public bool IsIntangible { get; set; }

    public bool IsAuc { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
