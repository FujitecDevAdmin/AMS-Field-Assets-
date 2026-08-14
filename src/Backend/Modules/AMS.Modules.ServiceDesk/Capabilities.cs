namespace AMS.Modules.ServiceDesk;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-9).
/// </summary>
/// <remarks>
/// A capability an endpoint declares but the seed does not contain can never be
/// granted, so the screen is unreachable. This is the fifth module to ship its
/// first slices with that gap open; the pattern is now written down in
/// docs/00DESIGNDECISIONS.md rather than rediscovered each time.
/// </remarks>
public static class Capabilities
{
    public static class ServiceDesk
    {
        /// <summary>Raise a ticket, a fault or a New Service request.</summary>
        /// <remarks>
        /// Separate from <see cref="Manage"/> because EVERY employee raises
        /// tickets and almost none of them work one. Merging the two would mean
        /// either giving the whole company the technician queue, or nobody a way
        /// to ask for help.
        /// </remarks>
        public const string Raise = "request.raise";

        /// <summary>Read the ticket queue and ticket detail.</summary>
        public const string View = "request.view";

        /// <summary>Work a ticket: start, hold, resolve, close, reopen.</summary>
        public const string Manage = "request.manage";

        /// <summary>Assign a ticket to a technician or a support team.</summary>
        /// <remarks>
        /// Separate again: a technician may pick up work, but handing it to
        /// somebody else is a decision about their day.
        /// </remarks>
        public const string Assign = "request.assign";

        /// <summary>Maintain request categories and sub-categories.</summary>
        public const string CategoryManage = "request-category.manage";

        /// <summary>Maintain support teams and their members.</summary>
        public const string TeamManage = "support-team.manage";

        /// <summary>Maintain reusable service request templates.</summary>
        public const string TemplateManage = "service-template.manage";

        /// <summary>Add a note to a ticket.</summary>
        public const string Note = "request.note";

        /// <summary>Send e-mail from a ticket.</summary>
        public const string Email = "request.email";

        /// <summary>Upload and download ticket attachments.</summary>
        public const string Attach = "request.attach";

        /// <summary>Define and publish approval workflows.</summary>
        public const string WorkflowManage = "approval-workflow.manage";

        /// <summary>Approve or reject an assigned approval step.</summary>
        public const string ApprovalDecide = "approval.decide";

        /// <summary>Cancel an approval run, with a recorded reason.</summary>
        public const string ApprovalCancel = "approval.cancel";
    }
}
