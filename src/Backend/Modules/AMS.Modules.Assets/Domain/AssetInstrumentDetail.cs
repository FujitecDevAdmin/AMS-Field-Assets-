namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetInstrumentDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetInstrumentDetail
{
    public int AssetId { get; set; }

    public DateOnly? CalibrationStartDate { get; set; }

    public DateOnly? CalibrationEndDate { get; set; }

    public int? CalibrationFrequencyMonths { get; set; }

    public string? CalibrationAgency { get; set; }

    public string? CertificateNumber { get; set; }

    public string? MeasurementRange { get; set; }

    public string? AccuracyClass { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
