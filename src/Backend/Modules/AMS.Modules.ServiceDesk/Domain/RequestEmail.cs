namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestEmail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestEmail
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    /// <summary>Defaults to <c>N'Outbound'</c>, as <c>DF_RequestEmail_Direction</c> does.</summary>
    public string Direction { get; set; } = "Outbound";

    public required string ToAddresses { get; set; }

    public string? CcAddresses { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    /// <summary>Defaults to <c>1</c>, as <c>DF_RequestEmail_IsHtml</c> does.</summary>
    public bool IsHtml { get; set; } = true;

    public required string Status { get; set; }

    public string? LastError { get; set; }

    public long? EmailOutboxId { get; set; }

    public int? SentByUserId { get; set; }

    public DateTime QueuedOnUtc { get; set; }

    public DateTime? SentOnUtc { get; set; }
}
