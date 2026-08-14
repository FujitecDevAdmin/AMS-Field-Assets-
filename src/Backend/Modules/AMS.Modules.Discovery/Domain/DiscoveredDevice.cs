namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// Mirrors <c>[Discovery].[DiscoveredDevice]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class DiscoveredDevice
{
    public int Id { get; set; }

    public required string Hostname { get; set; }

    public string? SerialNumber { get; set; }

    public string? Manufacturer { get; set; }

    public string? Model { get; set; }

    public string? OperatingSystem { get; set; }

    public string? MacAddress { get; set; }

    public string? RawPayloadJson { get; set; }

    public required string Status { get; set; }

    public int? LinkedAssetId { get; set; }

    public DateTime FirstSeenOnUtc { get; set; }

    public DateTime LastSeenOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
