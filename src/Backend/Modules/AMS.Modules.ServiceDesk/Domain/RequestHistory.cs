namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestHistory]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestHistory
{
    public long Id { get; set; }

    public int ServiceRequestId { get; set; }

    /// <summary>Defaults to <c>N'Transition'</c>, as <c>DF_RequestHistory_EntryKind</c> does.</summary>
    public string EntryKind { get; set; } = "Transition";

    public required string EntryText { get; set; }

    public string? Body { get; set; }

    public bool IsInternal { get; set; }

    public int? FromStatusId { get; set; }

    public int? ToStatusId { get; set; }

    public int? AssignedToUserId { get; set; }

    public int? RequestEmailId { get; set; }

    public DateTime OccurredOnUtc { get; set; }

    public required string PerformedBy { get; set; }
}
