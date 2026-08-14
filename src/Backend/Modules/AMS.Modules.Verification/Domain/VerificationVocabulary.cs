namespace AMS.Modules.Verification.Domain;

/// <summary>
/// What state an asset was found in.
/// </summary>
/// <remarks>
/// Spelled exactly as CK_PhysicalVerification_Condition allows (R2-20). Five
/// values and not a boolean, because "it is here but the screen is cracked" and
/// "it is not here at all" are different answers that lead to different work,
/// and a verification that could only say found or missing would collapse them.
/// </remarks>
public static class WorkingCondition
{
    /// <summary>Found, and fine.</summary>
    public const string Good = "Good";

    /// <summary>Found, marked, still usable.</summary>
    public const string MinorDamage = "MinorDamage";

    /// <summary>Found, and it needs attention before it is used again.</summary>
    public const string Damaged = "Damaged";

    /// <summary>Found, and it does not work.</summary>
    public const string NotWorking = "NotWorking";

    /// <summary>Not found. The one that starts an investigation.</summary>
    public const string Missing = "Missing";

    public static readonly string[] Allowed =
        [Good, MinorDamage, Damaged, NotWorking, Missing];

    /// <summary>The conditions that put a row on the exception report.</summary>
    /// <remarks>
    /// Everything except Good. A cracked screen nobody looks at is a cracked
    /// screen that becomes a missing asset.
    /// </remarks>
    public static readonly string[] Exceptions =
        [MinorDamage, Damaged, NotWorking, Missing];
}
