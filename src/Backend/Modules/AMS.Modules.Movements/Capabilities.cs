namespace AMS.Modules.Movements;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-6).
/// </summary>
/// <remarks>
/// A capability an endpoint declares but the seed does not contain can never be
/// granted to anybody, so the screen is simply unreachable. Add both together —
/// this is the third module to ship its first slices with that gap still open.
/// </remarks>
public static class Capabilities
{
    public static class Movements
    {
        /// <summary>Read shipments and the pending-receipt queue.</summary>
        public const string View = "movement.view";

        /// <summary>Despatch assets to another branch or to head office.</summary>
        public const string Manage = "movement.manage";

        /// <summary>Confirm arrival at the destination branch.</summary>
        /// <remarks>
        /// Separate from <see cref="Manage"/> because receiving is the
        /// DESTINATION's job. The person who despatched confirming their own
        /// arrival is what makes a goods receipt worthless.
        /// </remarks>
        public const string Receive = "movement.receive";

        /// <summary>Despatch branch standby stock to head office.</summary>
        public const string HandoverDispatch = "handover.dispatch";

        /// <summary>Record GRN receipt of assets arriving at head office.</summary>
        public const string HandoverReceive = "handover.receive";
    }
}
