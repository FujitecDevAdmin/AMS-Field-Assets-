namespace AMS.Modules.Organization.Domain;

/// <summary>
/// Mirrors <c>[Organization].[Branch]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class Branch
{
    public int Id { get; set; }

    public required string BranchCode { get; set; }

    public required string BranchName { get; set; }

    public int? RegionId { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    /// <summary>Defaults to <c>N'India Standard Time'</c>, as <c>DF_Branch_TimeZoneId</c> does.</summary>
    public string TimeZoneId { get; set; } = "India Standard Time";

    public bool IsHeadOffice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
