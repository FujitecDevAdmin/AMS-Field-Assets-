namespace AMS.Modules.Discovery.Features.ReportInventory;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record ReportInventoryRequest(
    string Hostname,
    string? SerialNumber,
    string? Manufacturer,
    string? Model,
    string? OperatingSystem,
    string? MacAddress,
    int? AssetId,
    ReportInventoryRequest.HealthReading? Health,
    IReadOnlyList<ReportInventoryRequest.SoftwareEntry>? Software,
    string? RawPayloadJson)
{
    /// <summary>One machine's vital signs, as the agent sends them.</summary>
    public sealed record HealthReading(
        decimal CpuPercent,
        decimal MemoryPercent,
        decimal SystemDrivePercent,
        decimal? BatteryHealthPercent,
        int UptimeHours,
        string? LoggedInUser);

    /// <summary>One installed program.</summary>
    public sealed record SoftwareEntry(string SoftwareName, string? Version, string? Publisher);
}
