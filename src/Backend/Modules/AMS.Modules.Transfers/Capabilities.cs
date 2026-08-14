namespace AMS.Modules.Transfers;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-8).
/// </summary>
/// <remarks>
/// Transfers had none seeded at all — the fourth module in a row to find that
/// gap. A capability an endpoint declares but the seed does not contain can
/// never be granted, so the screen is simply unreachable.
/// </remarks>
public static class Capabilities
{
    public static class Transfers
    {
        /// <summary>Read transfer requests and their SAP status.</summary>
        public const string View = "transfer.view";

        /// <summary>Raise a transfer of an asset.</summary>
        public const string Request = "transfer.request";

        /// <summary>Approve, reject or cancel a transfer request.</summary>
        /// <remarks>
        /// Separate from <see cref="Request"/> so the person who wants the
        /// transfer cannot grant it to themselves.
        /// </remarks>
        public const string Approve = "transfer.approve";

        /// <summary>Apply an approved transfer and queue it to SAP.</summary>
        /// <remarks>
        /// Separate again, because completing is the step that changes the
        /// register and puts a row in front of SAP. Approving says yes;
        /// completing makes it true.
        /// </remarks>
        public const string Complete = "transfer.complete";
    }
}
