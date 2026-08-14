namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetEvent]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetEvent
{
    public long Id { get; set; }

    public int AssetId { get; set; }

    public required string EventType { get; set; }

    public required string Description { get; set; }

    public DateTime EventOnUtc { get; set; }

    public required string PerformedBy { get; set; }

    public int? EmployeeId { get; set; }

    public string? EmployeeNameSnapshot { get; set; }

    public int? LocationId { get; set; }

    public string? LocationNameSnapshot { get; set; }

    public int? AllocationId { get; set; }

    public int? MovementId { get; set; }

    public int? ServiceRequestId { get; set; }

    public int? ContractId { get; set; }

    public int? HandoverId { get; set; }

    public int? VerificationId { get; set; }

    public int? DisposalId { get; set; }

    public decimal? QuantityDelta { get; set; }
}
