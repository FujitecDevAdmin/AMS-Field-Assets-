namespace AMS.Modules.Discovery.Features.ReportInventory;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class ReportInventoryMapper
{
    public static ReportInventoryCommand ToCommand(ReportInventoryRequest request, string? apiKey)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ReportInventoryCommand(
            apiKey,
            request.Hostname.Trim(),
            string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim(),
            string.IsNullOrWhiteSpace(request.Manufacturer) ? null : request.Manufacturer.Trim(),
            string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            string.IsNullOrWhiteSpace(request.OperatingSystem) ? null : request.OperatingSystem.Trim(),
            string.IsNullOrWhiteSpace(request.MacAddress) ? null : request.MacAddress.Trim(),
            request.AssetId,
            request.Health is null ? null : new ReportInventoryCommand.HealthReading(
                request.Health.CpuPercent,
                request.Health.MemoryPercent,
                request.Health.SystemDrivePercent,
                request.Health.BatteryHealthPercent,
                request.Health.UptimeHours,
                string.IsNullOrWhiteSpace(request.Health.LoggedInUser) ? null : request.Health.LoggedInUser.Trim()),
            [.. (request.Software ?? []).Select(s => new ReportInventoryCommand.SoftwareEntry(
                s.SoftwareName.Trim(),
                string.IsNullOrWhiteSpace(s.Version) ? null : s.Version.Trim(),
                string.IsNullOrWhiteSpace(s.Publisher) ? null : s.Publisher.Trim()))],
            request.RawPayloadJson);
    }
}
