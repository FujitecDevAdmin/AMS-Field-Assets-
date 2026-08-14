namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetVehicleDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetVehicleDetail
{
    public int AssetId { get; set; }

    public required string RegistrationNumber { get; set; }

    public string? ChassisNumber { get; set; }

    public string? EngineNumber { get; set; }

    public string? FuelType { get; set; }

    public DateOnly? FitnessExpiryDate { get; set; }

    public DateOnly? PucExpiryDate { get; set; }

    public DateOnly? InsuranceExpiryDate { get; set; }

    public int? OdometerKm { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
