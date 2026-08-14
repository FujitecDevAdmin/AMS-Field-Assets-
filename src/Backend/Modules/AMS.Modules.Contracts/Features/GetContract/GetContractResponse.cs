namespace AMS.Modules.Contracts.Features.GetContract;

/// <summary>
/// Everything the detail screen draws.
/// </summary>
/// <param name="Id">The contract.</param>
/// <param name="ContractNumber">What it is quoted by.</param>
/// <param name="ContractName">What it is called.</param>
/// <param name="ContractType">Amc, Warranty, Lease, Licence, Service or Insurance.</param>
/// <param name="VendorId">Organization.Vendor, id only (rule 2).</param>
/// <param name="VendorName">Resolved for display.</param>
/// <param name="StartDate">When cover began.</param>
/// <param name="EndDate">When it runs out.</param>
/// <param name="DaysToExpiry">Negative once it has. The number the screen colours.</param>
/// <param name="ContractValue">What it costs.</param>
/// <param name="LicensedSeats">For a licence.</param>
/// <param name="HasLicenceKey">Whether a key is stored. The key itself never leaves the database.</param>
/// <param name="AutoRenew">Whether it rolls over rather than lapsing.</param>
/// <param name="RenewalCount">How many times it has.</param>
/// <param name="Remarks">Anything else.</param>
/// <param name="AssetIds">Assets.Asset, ids only. What the contract covers.</param>
/// <param name="Documents">The scanned agreement, the purchase order, the certificate.</param>
/// <param name="Reminders">The windows that apply, and whether each is this contract's own.</param>
/// <param name="SentReminders">Which reminders actually went out, and when.</param>
public sealed record GetContractResponse(
    int Id,
    string ContractNumber,
    string ContractName,
    string ContractType,
    int? VendorId,
    string? VendorName,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysToExpiry,
    decimal? ContractValue,
    int? LicensedSeats,
    bool HasLicenceKey,
    bool AutoRenew,
    int RenewalCount,
    string? Remarks,
    IReadOnlyList<int> AssetIds,
    IReadOnlyList<GetContractResponse.Document> Documents,
    IReadOnlyList<GetContractResponse.ReminderWindow> Reminders,
    IReadOnlyList<GetContractResponse.SentReminder> SentReminders)
{
    /// <summary>One attached file.</summary>
    /// <param name="Id">The document row.</param>
    /// <param name="FileName">What to show.</param>
    /// <param name="ContentType">What it is.</param>
    /// <param name="SizeBytes">How big.</param>
    /// <param name="UploadedOnUtc">When.</param>
    public sealed record Document(
        int Id,
        string? FileName,
        string? ContentType,
        long? SizeBytes,
        DateTime UploadedOnUtc);

    /// <summary>One reminder window that applies to this contract.</summary>
    /// <param name="DaysBeforeExpiry">How long before the end date it goes out.</param>
    /// <param name="Recipients">Who to. Blank means the vendor contact.</param>
    /// <param name="Channel">Email, InApp or Both.</param>
    /// <param name="IsContractSpecific">
    /// True when this contract has its own setting, false when it is inheriting
    /// the organisation default. The screen needs to tell them apart: editing
    /// an inherited window creates an override, and somebody should know that
    /// before they do it.
    /// </param>
    public sealed record ReminderWindow(
        int DaysBeforeExpiry,
        string? Recipients,
        string Channel,
        bool IsContractSpecific);

    /// <summary>One reminder that actually went out.</summary>
    /// <param name="DaysBeforeExpiry">Which window it was.</param>
    /// <param name="ExpiryDateSnapshot">
    /// The end date it was measured against. Part of the unique key, so a
    /// renewed contract earns its reminders again for the NEW expiry (R2-2).
    /// </param>
    /// <param name="SentOnDate">When it went.</param>
    /// <param name="SentTo">Who to.</param>
    /// <param name="Outcome">Queued, Sent or Failed.</param>
    public sealed record SentReminder(
        int DaysBeforeExpiry,
        DateOnly ExpiryDateSnapshot,
        DateOnly SentOnDate,
        string? SentTo,
        string Outcome);
}
