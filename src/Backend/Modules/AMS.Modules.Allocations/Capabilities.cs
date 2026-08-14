namespace AMS.Modules.Allocations;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-5).
/// </summary>
/// <remarks>
/// A capability an endpoint declares but the seed does not contain can never
/// be granted to anybody, so the screen is simply unreachable. Add both
/// together — Identity learned that the hard way, and both `[Assets]` and this
/// module shipped their first slices with the gap still open.
/// </remarks>
public static class Capabilities
{
    public static class Allocations
    {
        /// <summary>Read allocations, expected returns and the overdue list.</summary>
        public const string View = "allocation.view";

        /// <summary>Allocate an asset to an employee and receive it back.</summary>
        public const string Manage = "allocation.manage";

        /// <summary>Raise a request for an asset to be allocated to an employee.</summary>
        public const string Request = "allocation.request";

        /// <summary>Approve or reject an allocation request.</summary>
        /// <remarks>
        /// Separate from <see cref="Manage"/> deliberately. The point of raising
        /// a request rather than allocating directly is that somebody else
        /// decides it; one person holding both makes the approval a formality.
        /// </remarks>
        public const string Approve = "allocation.approve";

        /// <summary>Reverse a return recorded in error.</summary>
        public const string RevertReturn = "allocation.revert-return";

        /// <summary>Countersign an employee's acknowledgement of an asset.</summary>
        /// <remarks>
        /// The reporting manager's, not an administrator's. Kept apart because
        /// the countersignature is worth nothing if the person who issued the
        /// asset can also sign it off.
        /// </remarks>
        public const string AcknowledgementApprove = "acknowledgement.approve";

        /// <summary>Accept an asset back from an employee into the branch store.</summary>
        public const string HandoverRecord = "handover.record";

        /// <summary>Maintain customer sites and map assets to them.</summary>
        public const string CustomerSiteManage = "customer-site.manage";
    }
}
