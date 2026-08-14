namespace AMS.Modules.Transfers.Domain;

/// <summary>
/// Mirrors <c>[Transfers].[AssetTransferRequest]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetTransferRequest
{
    public int Id { get; set; }

    public int AssetId { get; set; }

    public required string TransferType { get; set; }

    public required string Status { get; set; }

    public int? FromEmployeeId { get; set; }

    public int? ToEmployeeId { get; set; }

    public int? FromDepartmentId { get; set; }

    public int? ToDepartmentId { get; set; }

    public int? FromLocationId { get; set; }

    public int? ToLocationId { get; set; }

    public string? FromCostCenter { get; set; }

    public string? ToCostCenter { get; set; }

    public int RequestedByUserId { get; set; }

    public DateTime RequestedOnUtc { get; set; }

    public int? ApprovedByUserId { get; set; }

    public DateTime? ApprovedOnUtc { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public string? Remarks { get; set; }

    public int? MovementId { get; set; }

    public required string SapSyncStatus { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
