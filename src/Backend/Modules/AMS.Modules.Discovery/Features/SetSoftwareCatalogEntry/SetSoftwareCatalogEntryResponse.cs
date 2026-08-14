namespace AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;

/// <summary>
/// The entry, and how it stands against what is installed.
/// </summary>
/// <param name="Id">The catalogue entry.</param>
/// <param name="SoftwareName">The title, as the agent reports it.</param>
/// <param name="LicensedSeats">How many we bought.</param>
/// <param name="InstalledCount">How many machines have it.</param>
/// <param name="IsOverLicensed">Whether the second number is larger than the first.</param>
public sealed record SetSoftwareCatalogEntryResponse(
    int Id,
    string SoftwareName,
    int? LicensedSeats,
    int InstalledCount,
    bool IsOverLicensed);
