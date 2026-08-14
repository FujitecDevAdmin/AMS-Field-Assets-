namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[ChartOfAccount]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ChartOfAccount
{
    public int Id { get; set; }

    public required string CoaCode { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
