namespace AMS.Modules.ServiceLevel.Features.SearchEscalationLog;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchEscalationLogRequest(
    int? ServiceRequestId,
    string? Outcome,
    int? Take);
