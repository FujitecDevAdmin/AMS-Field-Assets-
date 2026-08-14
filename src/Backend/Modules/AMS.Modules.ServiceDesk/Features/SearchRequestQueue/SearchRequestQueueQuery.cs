using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchRequestQueue;

/// <summary>
/// The technician queue: overdue first, then nearest due. Catalogue: Service Request Queue.
/// </summary>
public sealed record SearchRequestQueueQuery(
    string? RequestKind,
    int? RequestStatusId,
    string? Priority,
    int? AssignedToUserId,
    int? AssignedTeamId,
    int? LocationId,
    bool Unassigned,
    bool OverdueOnly,
    bool OpenOnly,
    string? Search,
    int Skip,
    int Take) : IQuery<SearchRequestQueueResponse>;
