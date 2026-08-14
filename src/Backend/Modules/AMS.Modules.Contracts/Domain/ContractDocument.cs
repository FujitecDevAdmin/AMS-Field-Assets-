namespace AMS.Modules.Contracts.Domain;

/// <summary>
/// Mirrors <c>[Contracts].[ContractDocument]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ContractDocument
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public required string FilePath { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime UploadedOnUtc { get; set; }
}
