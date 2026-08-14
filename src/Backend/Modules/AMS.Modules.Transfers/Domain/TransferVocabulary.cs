namespace AMS.Modules.Transfers.Domain;

/// <summary>What is changing about the asset.</summary>
/// <remarks>
/// Constrained by CK_AssetTransferRequest_TypePair, which pairs each type with
/// the destination column it requires — so a Branch transfer with no
/// ToLocationId is refused by the database, not just by a form.
/// </remarks>
public static class TransferType
{
    /// <summary>Who holds it.</summary>
    public const string Employee = "Employee";

    /// <summary>Which department it belongs to.</summary>
    public const string Department = "Department";

    /// <summary>Which branch it sits at. This one usually causes a shipment too.</summary>
    public const string Branch = "Branch";

    /// <summary>Which cost centre carries it.</summary>
    public const string CostCenter = "CostCenter";

    /// <summary>The four the database allows.</summary>
    public static readonly string[] All = [Employee, Department, Branch, CostCenter];
}

/// <summary>Where a transfer request has got to.</summary>
/// <remarks>Spelled exactly as CK_AssetTransferRequest_Status allows (R3-7).</remarks>
public static class TransferStatus
{
    /// <summary>Raised, nobody has decided.</summary>
    public const string Pending = "Pending";

    /// <summary>Decided yes. The change has NOT been applied yet.</summary>
    public const string Approved = "Approved";

    /// <summary>Decided no.</summary>
    public const string Rejected = "Rejected";

    /// <summary>Applied to the register, and queued to SAP if it needs to go.</summary>
    public const string Completed = "Completed";

    /// <summary>Withdrawn before it was completed.</summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>Whether the accounting system has been told.</summary>
/// <remarks>
/// Spelled exactly as CK_AssetTransferRequest_SapSyncStatus allows (R3-7).
/// IX_AssetTransferRequest_SapPending is filtered on Pending, so the drain job
/// reads a narrow index rather than scanning every transfer ever made.
/// </remarks>
public static class SapSyncStatus
{
    /// <summary>Nothing to send — an employee or department move SAP does not track.</summary>
    public const string NotRequired = "NotRequired";

    /// <summary>Waiting for the sync job.</summary>
    public const string Pending = "Pending";

    /// <summary>SAP accepted it.</summary>
    public const string Sent = "Sent";

    /// <summary>SAP refused it. Retried, and visible on the screen until it is not failing.</summary>
    public const string Failed = "Failed";
}
