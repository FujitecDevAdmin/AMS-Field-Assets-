using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.ReportInventory;

/// <summary>
/// What an agent found on one machine. Posted by the agent, not by a person.
/// </summary>
public sealed record ReportInventoryCommand(
    string? ApiKey,
    string Hostname,
    string? SerialNumber,
    string? Manufacturer,
    string? Model,
    string? OperatingSystem,
    string? MacAddress,
    int? AssetId,
    ReportInventoryCommand.HealthReading? Health,
    IReadOnlyList<ReportInventoryCommand.SoftwareEntry> Software,
    string? RawPayloadJson) : ICommand<ReportInventoryResponse>
{
    /// <summary>One machine's vital signs.</summary>
    /// <param name="CpuPercent">How busy it is.</param>
    /// <param name="MemoryPercent">How full its memory is.</param>
    /// <param name="SystemDrivePercent">
    /// How full the system drive is. The one that turns into a ticket: a
    /// machine at 98 per cent stops installing updates and then stops working.
    /// </param>
    /// <param name="BatteryHealthPercent">For a laptop. Null for a desktop.</param>
    /// <param name="UptimeHours">How long since it was restarted.</param>
    /// <param name="LoggedInUser">Who was on it. Useful when the register disagrees.</param>
    public sealed record HealthReading(
        decimal CpuPercent,
        decimal MemoryPercent,
        decimal SystemDrivePercent,
        decimal? BatteryHealthPercent,
        int UptimeHours,
        string? LoggedInUser);

    /// <summary>One installed program.</summary>
    /// <param name="SoftwareName">As the machine reports it.</param>
    /// <param name="Version">Null when the machine does not say.</param>
    /// <param name="Publisher">Who wrote it.</param>
    public sealed record SoftwareEntry(string SoftwareName, string? Version, string? Publisher);
}
