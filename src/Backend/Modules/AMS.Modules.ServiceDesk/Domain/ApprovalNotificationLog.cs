namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ApprovalNotificationLog]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ApprovalNotificationLog
{
    public long Id { get; set; }

    public long RequestApprovalInstanceId { get; set; }

    public long? RequestApprovalStepId { get; set; }

    public long? RequestApprovalParticipantId { get; set; }

    public required string NotificationType { get; set; }

    public Guid IdempotencyKey { get; set; }

    public required string RecipientAddress { get; set; }

    public required string SubjectSnapshot { get; set; }

    public long? EmailOutboxId { get; set; }

    public required string Status { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public DateTime QueuedOnUtc { get; set; }

    public DateTime? SentOnUtc { get; set; }
}
