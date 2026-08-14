namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[ServiceRequest]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ServiceRequest
{
    public int Id { get; set; }

    public required string RequestNumber { get; set; }

    public required string RequestKind { get; set; }

    public required string Subject { get; set; }

    public string? Description { get; set; }

    public required string Priority { get; set; }

    public int RequestStatusId { get; set; }

    public int? RequestCategoryId { get; set; }

    public int? RequestSubCategoryId { get; set; }

    public int? ServiceTemplateId { get; set; }

    public int? AssetId { get; set; }

    public string? ManualAssetText { get; set; }

    public int RequestedByEmployeeId { get; set; }

    public int? OnBehalfOfEmployeeId { get; set; }

    public int? LocationId { get; set; }

    public int? AssignedToUserId { get; set; }

    public int? AssignedTeamId { get; set; }

    public DateTime? AssignedOnUtc { get; set; }

    public DateTime? ResolvedOnUtc { get; set; }

    public DateTime? ClosedOnUtc { get; set; }

    public string? Resolution { get; set; }

    public int? SlaPolicyId { get; set; }

    public DateTime? SlaStartOnUtc { get; set; }

    public bool IsScheduledHold { get; set; }

    public DateTime? NextOperationalStartUtc { get; set; }

    public string? ScheduleHoldReason { get; set; }

    public DateTime? ResponseDueOnUtc { get; set; }

    public DateTime? ResolutionDueOnUtc { get; set; }

    public DateTime? FirstResponseOnUtc { get; set; }

    public int? ResponseElapsedMinutes { get; set; }

    public int ResolutionConsumedMinutes { get; set; }

    public int TechnicianWorkingMinutes { get; set; }

    public int SlaPausedMinutes { get; set; }

    public DateTime? SlaLastCalculatedOnUtc { get; set; }

    public bool IsSlaPaused { get; set; }

    public bool IsSlaOverdue { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>
    public byte[] RowVersion { get; set; } = [];
}
