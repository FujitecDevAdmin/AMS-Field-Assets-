namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>What kind of thing somebody is asking for.</summary>
/// <remarks>
/// Spelled exactly as CK_ServiceRequest_Kind and CK_ServiceTemplate_Kind allow
/// (R2-18). One pipeline carries all three, which is why the kind is a column
/// rather than three tables.
/// </remarks>
public static class RequestKind
{
    /// <summary>A general IT problem with no particular asset.</summary>
    public const string SupportTicket = "SupportTicket";

    /// <summary>A fault on an asset the requester holds.</summary>
    public const string AssetIssue = "AssetIssue";

    /// <summary>A joiner, a machine, an access grant. This one goes through approval.</summary>
    public const string NewService = "NewService";

    /// <summary>The three the database allows.</summary>
    public static readonly string[] All = [SupportTicket, AssetIssue, NewService];
}

/// <summary>How urgent the requester says it is.</summary>
/// <remarks>Spelled exactly as CK_ServiceRequest_Priority allows.</remarks>
public static class RequestPriority
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";

    /// <summary>Added by the design: the handbook's priority list omitted it.</summary>
    public const string Critical = "Critical";

    /// <summary>The four the database allows.</summary>
    public static readonly string[] All = [Low, Medium, High, Critical];
}

/// <summary>What a line in the ticket's timeline is.</summary>
/// <remarks>
/// Spelled exactly as CK_RequestHistory_EntryKind allows. One timeline, not a
/// second notes table nobody joins: a private note, an e-mail and an automatic
/// SLA activation all land in the list the handbook calls Conversations and
/// History.
/// </remarks>
public static class HistoryEntryKind
{
    /// <summary>A status change.</summary>
    public const string Transition = "Transition";

    /// <summary>Somebody wrote something.</summary>
    public const string Note = "Note";

    /// <summary>A message left or arrived.</summary>
    public const string Email = "Email";

    /// <summary>The system did it, not a person.</summary>
    public const string Automation = "Automation";

    /// <summary>The clock started, paused, resumed or blew.</summary>
    public const string Sla = "Sla";

    /// <summary>It went up a level.</summary>
    public const string Escalation = "Escalation";

    /// <summary>The six the database allows.</summary>
    public static readonly string[] All =
        [Transition, Note, Email, Automation, Sla, Escalation];
}

/// <summary>Why a file is on the ticket.</summary>
/// <remarks>Spelled exactly as CK_RequestAttachment_Type allows.</remarks>
public static class AttachmentKind
{
    /// <summary>The requester attached it when raising.</summary>
    public const string Requester = "Requester";

    /// <summary>Evidence of the fix.</summary>
    public const string Resolution = "Resolution";

    /// <summary>It came with, or went with, an e-mail.</summary>
    public const string Email = "Email";

    /// <summary>The three the database allows.</summary>
    public static readonly string[] All = [Requester, Resolution, Email];
}

/// <summary>Which way a message travelled.</summary>
public static class EmailDirection
{
    public const string Outbound = "Outbound";

    /// <summary>
    /// A reply that arrived. It has no sending user, which is why
    /// CK_RequestEmail_SentBy demands one only for Outbound.
    /// </summary>
    public const string Inbound = "Inbound";
}

/// <summary>How far a message has got.</summary>
/// <remarks>
/// Queued means written down; Sent means an SMTP server accepted it. That is
/// not the same as it reaching an inbox, and the column records what we know
/// rather than what we hope.
/// </remarks>
public static class EmailStatus
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

/// <summary>What a status does to the resolution clock.</summary>
/// <remarks>
/// Spelled exactly as CK_RequestStatus_SlaClockBehaviour allows. It is a column
/// on RequestStatus and not a hard-coded list here, because "Awaiting Vendor"
/// must be addable without a release and the clock has to know.
/// </remarks>
public static class SlaClockBehaviour
{
    /// <summary>Time counts.</summary>
    public const string Running = "Running";

    /// <summary>Time is frozen: waiting on somebody who is not us.</summary>
    public const string Paused = "Paused";

    /// <summary>The clock is finished. Resolved, Closed, Rejected.</summary>
    public const string Stopped = "Stopped";
}
