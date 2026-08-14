using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.SearchEscalationLog;

/// <summary>
/// Which escalations actually fired. Catalogue: the SLA panel on Request Detail.
/// </summary>
public sealed record SearchEscalationLogQuery(
    int? ServiceRequestId,
    string? Outcome,
    int Take) : IQuery<SearchEscalationLogResponse>;
