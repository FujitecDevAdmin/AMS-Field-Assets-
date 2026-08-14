using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SearchMyApprovals;

/// <summary>
/// What is waiting on me. Catalogue: My Approvals.
/// </summary>
/// <remarks>
/// Only levels whose turn has actually come. A participant on level three of a
/// route still sitting on level one is not being asked for anything yet, and
/// showing it would train people to ignore the screen.
/// </remarks>
public sealed class SearchMyApprovalsHandler(ServiceDeskDbContext db, IClock clock)
    : IRequestHandler<SearchMyApprovalsQuery, SearchMyApprovalsResponse>
{
    public async Task<Result<SearchMyApprovalsResponse>> HandleAsync(
        SearchMyApprovalsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = clock.UtcNow;

        var query =
            from p in db.RequestApprovalParticipants
            join s in db.RequestApprovalSteps on p.RequestApprovalStepId equals s.Id
            join i in db.RequestApprovalInstances on s.RequestApprovalInstanceId equals i.Id
            join r in db.ServiceRequests on i.ServiceRequestId equals r.Id
            where p.ApproverUserId == request.UserId
            select new { Participant = p, Step = s, Instance = i, Request = r };

        if (request.PendingOnly)
        {
            query = query.Where(x =>
                x.Participant.ParticipantStatus == ParticipantStatus.Pending
                && x.Step.Status == ApprovalStepStatus.Pending);
        }

        var total = await query.CountAsync(ct);

        var overdue = await query.CountAsync(
            x => x.Step.Status == ApprovalStepStatus.Pending
                && x.Step.DueOnUtc != null
                && x.Step.DueOnUtc < now, ct);

        var rows = await query
            // Late first, then nearest due, then oldest. The same order as the
            // ticket queue and for the same reason: the screen is a list of
            // what to do next.
            .OrderByDescending(x => x.Step.DueOnUtc != null && x.Step.DueOnUtc < now)
            .ThenBy(x => x.Step.DueOnUtc == null)
            .ThenBy(x => x.Step.DueOnUtc)
            .ThenBy(x => x.Instance.SubmittedOnUtc)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(x => new SearchMyApprovalsResponse.Row(
                x.Participant.Id,
                x.Instance.Id,
                x.Request.Id,
                x.Request.RequestNumber,
                x.Request.Subject,
                x.Request.Priority,
                x.Step.StageNumber,
                x.Step.StageNameSnapshot,
                x.Step.ApprovalModeSnapshot,
                x.Participant.ParticipantStatus,
                x.Step.ActivatedOnUtc,
                x.Step.DueOnUtc,
                x.Step.Status == ApprovalStepStatus.Pending
                    && x.Step.DueOnUtc != null
                    && x.Step.DueOnUtc < now,
                x.Instance.SubmittedOnUtc))
            .ToListAsync(ct);

        return new SearchMyApprovalsResponse(rows, total, overdue);
    }
}
