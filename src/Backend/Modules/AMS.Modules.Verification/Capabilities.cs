namespace AMS.Modules.Verification;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-12).
/// </summary>
/// <remarks>
/// Run is separate from manage because they are different people in different
/// places: a technician walks a branch with a phone, and an administrator opens
/// and closes the cycle from a desk. Giving the technician the power to close a
/// cycle mid-count is how a count ends early.
/// </remarks>
public static class Capabilities
{
    public static class Verification
    {
        /// <summary>Record a sighting or a bulk count against the open cycle.</summary>
        public const string Run = "verification.run";

        /// <summary>Read verification results and the exception report.</summary>
        public const string View = "verification.view";

        /// <summary>Open and close verification cycles.</summary>
        public const string Manage = "verification.manage";
    }
}
