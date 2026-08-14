namespace AMS.Modules.Notifications.Domain;

/// <summary>
/// Mirrors <c>[Notifications].[EmailOutbox]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class EmailOutbox
{
    public long Id { get; set; }

    public required string ToAddress { get; set; }

    public string? CcAddress { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    public bool IsHtml { get; set; }

    public required string Status { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }

    public string? SourceType { get; set; }

    public long? SourceId { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? SentOnUtc { get; set; }
}
