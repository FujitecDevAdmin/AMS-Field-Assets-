namespace AMS.Modules.Contracts.Domain;

/// <summary>
/// Mirrors <c>[Contracts].[Contract]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
/// <remarks>
/// System-versioned. Prior versions live in <c>[Contracts].[ContractHistory]</c>,
/// readable with <c>TemporalAsOf</c>. The concurrency token is
/// <c>ConcurrencyStamp</c>, NOT the period columns (R2-22).
/// </remarks>
public sealed class Contract
{
    public int Id { get; set; }

    public required string ContractNumber { get; set; }

    public required string ContractName { get; set; }

    public required string ContractType { get; set; }

    public int? VendorId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal? ContractValue { get; set; }

    public int? LicensedSeats { get; set; }

    public byte[]? LicenseKeyEncrypted { get; set; }

    public bool AutoRenew { get; set; }

    public int RenewalCount { get; set; }

    public string? Remarks { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    public Guid ConcurrencyStamp { get; set; }
}
