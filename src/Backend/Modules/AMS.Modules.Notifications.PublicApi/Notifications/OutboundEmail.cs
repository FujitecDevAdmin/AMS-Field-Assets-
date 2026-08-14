namespace AMS.Modules.Notifications.PublicApi.Notifications;

/// <summary>A message to be sent, once somebody gets round to it.</summary>
/// <param name="ToAddress">Where it goes.</param>
/// <param name="CcAddress">Who else sees it. A ticket reply copies the branch admin.</param>
/// <param name="Subject">The subject line.</param>
/// <param name="Body">The message. HTML or not, per <paramref name="IsHtml"/>.</param>
/// <param name="IsHtml">Whether the body is markup.</param>
/// <param name="SourceType">
/// What asked for it: ServiceRequest, Contract, SlaEscalation. Recorded so a
/// failed send can be traced back to the thing that wanted it — otherwise a
/// bounced address is a row in a queue with nothing attached to it.
/// </param>
/// <param name="SourceId">The id of that thing.</param>
public sealed record OutboundEmail(
    string ToAddress,
    string? CcAddress,
    string Subject,
    string Body,
    bool IsHtml,
    string? SourceType,
    long? SourceId);

/// <summary>What asked for a message. IX_EmailOutbox_Source reads these.</summary>
/// <remarks>
/// Spelled here rather than in each caller, because the value of the column is
/// that a person can filter on it, and three modules spelling it three ways
/// makes it a column nobody filters on.
/// </remarks>
public static class EmailSource
{
    /// <summary>A reply sent from a ticket.</summary>
    public const string ServiceRequest = "ServiceRequest";

    /// <summary>A renewal or expiry reminder.</summary>
    public const string Contract = "Contract";

    /// <summary>A target was missed and somebody is being told.</summary>
    public const string SlaEscalation = "SlaEscalation";

    /// <summary>Somebody is being asked to approve something.</summary>
    public const string Approval = "Approval";
}
