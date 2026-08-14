namespace AMS.Modules.Audit.Domain;

/// <summary>
/// Mirrors <c>[Audit].[ScheduledFieldChange]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ScheduledFieldChange
{
    public int Id { get; set; }

    public required string SchemaName { get; set; }

    public required string EntityName { get; set; }

    public required string EntityId { get; set; }

    public required string FieldName { get; set; }

    public string? CurrentValue { get; set; }

    public string? NewValue { get; set; }

    public DateOnly EffectiveFromDate { get; set; }

    public DateOnly? EffectiveToDate { get; set; }

    public required string Status { get; set; }

    public required string Reason { get; set; }

    public int RequestedByUserId { get; set; }

    public DateTime RequestedOnUtc { get; set; }

    public DateTime? AppliedOnUtc { get; set; }

    public string? AppliedBy { get; set; }

    public DateTime? CancelledOnUtc { get; set; }

    public int? CancelledByUserId { get; set; }

    public string? FailureReason { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
