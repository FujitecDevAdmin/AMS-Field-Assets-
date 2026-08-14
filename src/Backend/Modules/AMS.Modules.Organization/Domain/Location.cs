namespace AMS.Modules.Organization.Domain;

/// <summary>
/// Mirrors <c>[Organization].[Location]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class Location
{
    public int Id { get; set; }

    public required string LocationCode { get; set; }

    public required string LocationName { get; set; }

    public int? RegionId { get; set; }

    /// <summary>Defaults to <c>N'India Standard Time'</c>, as <c>DF_Location_TimeZoneId</c> does.</summary>
    public string TimeZoneId { get; set; } = "India Standard Time";

    public bool IsHeadOffice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
