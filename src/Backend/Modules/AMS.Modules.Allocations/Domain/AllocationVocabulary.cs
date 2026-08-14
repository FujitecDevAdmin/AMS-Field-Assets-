namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Where an allocation request has got to.
/// </summary>
/// <remarks>
/// Spelled exactly as CK_AssetAllocationApproval_Status allows. Constants and
/// not an enum because the column is nvarchar: the database is the authority on
/// the vocabulary, and an enum would put a second, silently divergent copy of
/// it in C#.
/// </remarks>
public static class ApprovalStatus
{
    /// <summary>Raised, nobody has decided.</summary>
    public const string Pending = "Pending";

    /// <summary>The branch agreed; head office has not yet.</summary>
    public const string BranchApproved = "BranchApproved";

    /// <summary>Decided yes. The asset may now be allocated - it is not yet.</summary>
    public const string Approved = "Approved";

    /// <summary>Decided no. The remark saying why stays on the record.</summary>
    public const string Rejected = "Rejected";

    /// <summary>Withdrawn by the person who raised it.</summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>How far an employee's acknowledgement has got.</summary>
/// <remarks>Spelled exactly as CK_AssetAcknowledgement_Status allows.</remarks>
public static class AcknowledgementStatus
{
    /// <summary>Created with the allocation. The employee has not signed.</summary>
    public const string Pending = "Pending";

    /// <summary>The employee signed. The manager has not countersigned.</summary>
    public const string Signed = "Signed";

    /// <summary>The reporting manager countersigned. The undertaking is complete.</summary>
    public const string Approved = "Approved";
}

/// <summary>Where a handed-back asset physically is.</summary>
/// <remarks>Spelled exactly as CK_AssetHandover_Status allows.</remarks>
public static class HandoverStatus
{
    /// <summary>In the branch store.</summary>
    public const string HandedOver = "HandedOver";

    /// <summary>Despatched, not yet received at head office.</summary>
    public const string InTransitToHo = "InTransitToHo";

    /// <summary>Received at head office by GRN.</summary>
    public const string ReceivedAtHo = "ReceivedAtHo";

    /// <summary>Reversed before despatch.</summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>
/// What state an asset came back in.
/// </summary>
/// <remarks>
/// Spelled exactly as CK_AssetHandover_Condition allows, and the same five
/// words CK_PhysicalVerification_Condition uses. One vocabulary for "what
/// state is this in", whoever is looking at it.
/// </remarks>
public static class ReturnCondition
{
    public const string Good = "Good";
    public const string MinorDamage = "MinorDamage";
    public const string Damaged = "Damaged";
    public const string NotWorking = "NotWorking";
    public const string Missing = "Missing";

    /// <summary>The five the database allows.</summary>
    public static readonly string[] All =
        [Good, MinorDamage, Damaged, NotWorking, Missing];
}
