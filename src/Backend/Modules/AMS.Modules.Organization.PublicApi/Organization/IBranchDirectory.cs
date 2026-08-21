namespace AMS.Modules.Organization.PublicApi.Organization;

/// <summary>Where a branch is, in the only sense other modules need.</summary>
/// <remarks>
/// Separate from <see cref="IEmployeeDirectory"/> because the consumer is
/// different: the operational calendar needs a branch's time zone and nothing
/// about its people. A branch opens at 09:00 where it stands, and the only way
/// to turn that into an instant is to know which 09:00 is meant.
/// </remarks>
public interface IBranchDirectory
{
    /// <summary>
    /// The branch's time zone id, or null if the branch is unknown.
    /// </summary>
    /// <remarks>
    /// A Windows time zone id — <c>India Standard Time</c> — because that is
    /// what <c>Organization.Branch.TimeZoneId</c> stores and defaults to.
    /// </remarks>
    Task<string?> TimeZoneOfAsync(int branchId, CancellationToken ct);

    /// <summary>Whether a branch exists and is in use.</summary>
    Task<bool> IsActiveAsync(int branchId, CancellationToken ct);

    /// <summary>Active Branch Master records matching the supplied ids.</summary>
    Task<IReadOnlyList<BranchReference>> FindActiveAsync(
        IReadOnlyCollection<int> branchIds,
        CancellationToken ct);

    /// <summary>All active Branch Master records, ordered for selection.</summary>
    Task<IReadOnlyList<BranchReference>> ListActiveAsync(CancellationToken ct);
}

/// <summary>A Branch Master identity used for cross-module matching.</summary>
/// <param name="Id">Branch id.</param>
/// <param name="BranchCode">Stable business code.</param>
/// <param name="BranchName">Displayed and imported name.</param>
public sealed record BranchReference(int Id, string BranchCode, string BranchName);

/// <summary>Who to write to at a vendor.</summary>
/// <param name="VendorName">Their name, for the message.</param>
/// <param name="ContactPerson">The person, if one is recorded.</param>
/// <param name="Email">Where to write. Null when nobody recorded an address.</param>
public sealed record VendorContact(string VendorName, string? ContactPerson, string? Email);

/// <summary>Organization's answer to "who supplies this".</summary>
/// <remarks>
/// One reader of <c>Vendor</c>, not several. Contract reminders want a vendor's
/// address; so, later, will purchase orders and warranty claims.
/// </remarks>
public interface IVendorDirectory
{
    /// <summary>One vendor, or null if the id is unknown.</summary>
    Task<VendorContact?> FindAsync(int vendorId, CancellationToken ct);

    /// <summary>Whether a vendor exists and is still in use.</summary>
    Task<bool> IsActiveAsync(int vendorId, CancellationToken ct);
}
