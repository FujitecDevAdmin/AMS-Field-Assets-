namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// Mirrors <c>[Discovery].[SoftwareCatalog]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SoftwareCatalog
{
    public int Id { get; set; }

    public required string SoftwareName { get; set; }

    public string? Publisher { get; set; }

    public int? LicensedSeats { get; set; }

    public int? ContractId { get; set; }

    public bool IsBlacklisted { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
