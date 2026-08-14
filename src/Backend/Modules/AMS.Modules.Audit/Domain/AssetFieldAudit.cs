namespace AMS.Modules.Audit.Domain;

/// <summary>
/// Mirrors <c>[Audit].[AssetFieldAudit]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetFieldAudit
{
    public long Id { get; set; }

    public required string EntityName { get; set; }

    public required string EntityId { get; set; }

    public int? AssetId { get; set; }

    public required string FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedOnUtc { get; set; }

    public required string ChangedBy { get; set; }
}
