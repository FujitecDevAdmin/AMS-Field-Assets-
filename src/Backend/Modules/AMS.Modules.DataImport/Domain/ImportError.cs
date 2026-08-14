namespace AMS.Modules.DataImport.Domain;

/// <summary>
/// Mirrors <c>[DataImport].[ImportError]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ImportError
{
    public long Id { get; set; }

    public int ImportBatchId { get; set; }

    public int RowNumber { get; set; }

    public string? ColumnName { get; set; }

    public string? RawValue { get; set; }

    public required string ErrorCode { get; set; }

    public required string ErrorMessage { get; set; }

    public bool IsResolved { get; set; }

    public DateTime RecordedOnUtc { get; set; }
}
