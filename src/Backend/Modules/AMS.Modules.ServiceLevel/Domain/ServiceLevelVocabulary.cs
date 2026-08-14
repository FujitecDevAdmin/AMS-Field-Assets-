namespace AMS.Modules.ServiceLevel.Domain;

/// <summary>
/// How urgent a ticket is, as CK_SlaPolicy_Priority allows.
/// </summary>
/// <remarks>
/// The same four words ServiceDesk uses, spelled again here rather than shared.
/// Rule 2: a constant one module reads out of another is a reference, and the
/// two vocabularies happening to agree is a fact about the design script, which
/// both modules read.
/// </remarks>
public static class SlaPriority
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly string[] Allowed = [Low, Medium, High, Critical];
}

/// <summary>Why a day is a holiday. Spelled as CK_HolidayCalendar_Type allows.</summary>
/// <remarks>
/// The type is recorded rather than inferred because the four are treated
/// differently by people even though the calendar treats them the same: an
/// Optional holiday is one a branch may choose to work, and a report that
/// cannot tell it from Republic Day cannot explain a difference anybody asks
/// about.
/// </remarks>
public static class HolidayType
{
    /// <summary>A public holiday. Republic Day, Independence Day.</summary>
    public const string Government = "Government";

    /// <summary>Diwali, Pongal, Christmas.</summary>
    public const string Festival = "Festival";

    /// <summary>Observed in some states and not others.</summary>
    public const string Regional = "Regional";

    /// <summary>A branch may work it. Still a holiday to the calendar.</summary>
    public const string Optional = "Optional";

    public static readonly string[] Allowed = [Government, Festival, Regional, Optional];
}

/// <summary>Which target an escalation ladder is about. CK_SlaEscalation_Type.</summary>
public static class EscalationType
{
    /// <summary>Nobody replied in time.</summary>
    public const string Response = "Response";

    /// <summary>Nobody fixed it in time.</summary>
    public const string Resolution = "Resolution";

    public static readonly string[] Allowed = [Response, Resolution];
}

/// <summary>Who gets told. CK_SlaEscalation_RecipientType.</summary>
public static class EscalationRecipient
{
    public const string AssignedTechnician = "AssignedTechnician";
    public const string TeamLead = "TeamLead";
    public const string BranchAdmin = "BranchAdmin";

    /// <summary>The requester's manager.</summary>
    public const string Manager = "Manager";

    /// <summary>A fixed address. RecipientAddress is required, and CK_SlaEscalation_CustomAddress says so.</summary>
    public const string Custom = "Custom";

    public static readonly string[] Allowed =
        [AssignedTechnician, TeamLead, BranchAdmin, Manager, Custom];
}

/// <summary>How an escalation reaches somebody. CK_SlaEscalation_Channel.</summary>
public static class EscalationChannel
{
    public const string Email = "Email";
    public const string InApp = "InApp";
    public const string Both = "Both";

    public static readonly string[] Allowed = [Email, InApp, Both];
}

/// <summary>What became of an escalation attempt. CK_SlaEscalationLog_Outcome.</summary>
/// <remarks>
/// Failed is the interesting one: UX_SlaEscalationLog_OncePerLevel excludes it
/// (R2-3), so a failed queue attempt can be retried while a Sent or Skipped row
/// still blocks a repeat. Without that the monitor would send 1,440 e-mails a
/// day about one overdue ticket and everybody would filter the address.
/// </remarks>
public static class EscalationOutcome
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Failed = "Failed";

    /// <summary>Deliberately not sent — no recipient could be found, say.</summary>
    public const string Skipped = "Skipped";

    public static readonly string[] Allowed = [Queued, Sent, Failed, Skipped];
}
