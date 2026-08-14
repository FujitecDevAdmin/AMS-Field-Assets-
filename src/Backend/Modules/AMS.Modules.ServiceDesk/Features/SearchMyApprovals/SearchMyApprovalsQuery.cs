using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchMyApprovals;

/// <summary>
/// What is waiting on me. Catalogue: My Approvals.
/// </summary>
public sealed record SearchMyApprovalsQuery(
    int UserId,
    bool PendingOnly,
    int Skip,
    int Take) : IQuery<SearchMyApprovalsResponse>;
