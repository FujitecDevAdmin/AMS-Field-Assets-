namespace AMS.Modules.Discovery.Features.SearchInstalledSoftware;

/// <summary>
/// One row per title, most installed first.
/// </summary>
/// <param name="Rows">The titles, with how many machines have each.</param>
/// <param name="BlacklistedInstallCount">Installations of software nobody is meant to have.</param>
/// <param name="OverLicensedTitleCount">Titles installed on more machines than there are seats.</param>
public sealed record SearchInstalledSoftwareResponse(
    IReadOnlyList<SearchInstalledSoftwareResponse.Row> Rows,
    int BlacklistedInstallCount,
    int OverLicensedTitleCount)
{
    /// <summary>One title, across the estate.</summary>
    /// <param name="SoftwareName">As the agent reports it.</param>
    /// <param name="Publisher">Who wrote it.</param>
    /// <param name="InstalledCount">How many machines have it.</param>
    /// <param name="LicensedSeats">How many we bought, when the catalogue says.</param>
    /// <param name="IsOverLicensed">
    /// Whether it is on more machines than there are seats. The number a vendor
    /// audit asks about.
    /// </param>
    /// <param name="IsBlacklisted">Whether nobody is meant to have it.</param>
    /// <param name="IsInCatalogue">
    /// False when nobody has said anything about this title. Not the same as
    /// unlicensed: it means undecided, and a screen that showed the two the
    /// same way would make every new title look like a breach.
    /// </param>
    /// <param name="ContractId">The licence contract, when one is recorded.</param>
    public sealed record Row(
        string SoftwareName,
        string? Publisher,
        int InstalledCount,
        int? LicensedSeats,
        bool IsOverLicensed,
        bool IsBlacklisted,
        bool IsInCatalogue,
        int? ContractId);
}
