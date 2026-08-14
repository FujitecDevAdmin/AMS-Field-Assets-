namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestStatus]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestStatus
{
    public int Id { get; set; }

    public required string StatusName { get; set; }

    public bool IsClosedState { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Defaults to <c>N'Running'</c>, as <c>DF_RequestStatus_SlaClockBehaviour</c> does.</summary>
    public string SlaClockBehaviour { get; set; } = "Running";

    public bool CountsTechnicianTime { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
