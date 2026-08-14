namespace AMS.Modules.DataImport.Domain;

/// <summary>
/// Mirrors <c>[DataImport].[ImportBatch]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ImportBatch
{
    public int Id { get; set; }

    public required string BatchNumber { get; set; }

    public required string ImportType { get; set; }

    public required string FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileHash { get; set; }

    public bool IsDryRun { get; set; }

    public required string Status { get; set; }

    public int TotalRows { get; set; }

    public int SucceededRows { get; set; }

    public int FailedRows { get; set; }

    public int ImportedByUserId { get; set; }

    public DateTime StartedOnUtc { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
