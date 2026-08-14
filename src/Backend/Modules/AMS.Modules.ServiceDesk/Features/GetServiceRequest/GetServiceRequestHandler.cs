using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.GetServiceRequest;

/// <summary>
/// One ticket, everything on it. Catalogue: Request Detail.
/// </summary>
/// <remarks>
/// Four reads rather than one join: a ticket with forty history entries and
/// three files would otherwise come back as a hundred and twenty rows carrying
/// the same ticket over and over, and the screen would take them apart again.
/// </remarks>
public sealed class GetServiceRequestHandler(ServiceDeskDbContext db)
    : IRequestHandler<GetServiceRequestQuery, GetServiceRequestResponse>
{
    public async Task<Result<GetServiceRequestResponse>> HandleAsync(
        GetServiceRequestQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var head = await (
            from r in db.ServiceRequests
            join s in db.RequestStatuses on r.RequestStatusId equals s.Id
            where r.Id == request.Id
            select new { Request = r, Status = s })
            .SingleOrDefaultAsync(ct);

        if (head is null)
        {
            return Error.NotFound("ServiceRequest", request.Id);
        }

        var ticket = head.Request;

        var categoryName = await db.RequestCategories
            .Where(c => c.Id == ticket.RequestCategoryId)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync(ct);

        var subCategoryName = await db.RequestSubCategories
            .Where(s => s.Id == ticket.RequestSubCategoryId)
            .Select(s => s.SubCategoryName)
            .FirstOrDefaultAsync(ct);

        var teamName = await db.SupportTeams
            .Where(t => t.Id == ticket.AssignedTeamId)
            .Select(t => t.TeamName)
            .FirstOrDefaultAsync(ct);

        var history = db.RequestHistories.Where(h => h.ServiceRequestId == ticket.Id);

        // An internal note is hidden from the requester, not from the record.
        // The caller decides which view they are drawing; audit reads the
        // table.
        if (!request.IncludeInternal)
        {
            history = history.Where(h => !h.IsInternal);
        }

        var entries = await history
            .OrderBy(h => h.OccurredOnUtc)
            .ThenBy(h => h.Id)
            .Select(h => new GetServiceRequestResponse.HistoryEntry(
                h.Id, h.EntryKind, h.EntryText, h.Body, h.IsInternal,
                h.FromStatusId, h.ToStatusId, h.RequestEmailId, h.OccurredOnUtc, h.PerformedBy))
            .ToListAsync(ct);

        var attachments = await db.RequestAttachments
            .Where(a => a.ServiceRequestId == ticket.Id)
            .OrderBy(a => a.UploadedOnUtc)
            .ThenBy(a => a.Id)
            .Select(a => new GetServiceRequestResponse.Attachment(
                a.Id, a.AttachmentType, a.FileName, a.ContentType, a.SizeBytes,
                a.RequestEmailId, a.UploadedOnUtc))
            .ToListAsync(ct);

        GetServiceRequestResponse.NewServiceDetail? newService = null;

        if (ticket.RequestKind == RequestKind.NewService)
        {
            var detail = await db.NewServiceRequestDetails
                .SingleOrDefaultAsync(d => d.ServiceRequestId == ticket.Id, ct);

            if (detail is not null)
            {
                var items = await db.NewServiceRequestItems
                    .Where(i => i.ServiceRequestId == ticket.Id)
                    .OrderBy(i => i.Id)
                    .Select(i => new GetServiceRequestResponse.NewServiceItem(
                        i.AssetTypeId, i.Quantity, i.Specification))
                    .ToListAsync(ct);

                newService = new GetServiceRequestResponse.NewServiceDetail(
                    detail.NeedsEmail, detail.NeedsErp, detail.NeedsDms, detail.NeedsVpn,
                    detail.RequiredByDate, detail.Notes, items);
            }
        }

        return new GetServiceRequestResponse(
            ticket.Id,
            ticket.RequestNumber,
            ticket.RequestKind,
            ticket.Subject,
            ticket.Description,
            ticket.Priority,
            ticket.RequestStatusId,
            head.Status.StatusName,
            head.Status.IsClosedState,
            ticket.RequestCategoryId,
            categoryName,
            ticket.RequestSubCategoryId,
            subCategoryName,
            ticket.AssetId,
            ticket.ManualAssetText,
            ticket.RequestedByEmployeeId,
            ticket.OnBehalfOfEmployeeId,
            ticket.LocationId,
            ticket.AssignedToUserId,
            ticket.AssignedTeamId,
            teamName,
            ticket.AssignedOnUtc,
            ticket.ResolvedOnUtc,
            ticket.ClosedOnUtc,
            ticket.Resolution,
            ticket.ResponseDueOnUtc,
            ticket.ResolutionDueOnUtc,
            ticket.FirstResponseOnUtc,
            ticket.IsSlaPaused,
            ticket.IsSlaOverdue,
            ticket.ResolutionConsumedMinutes,
            ticket.CreatedOnUtc,
            newService,
            entries,
            attachments);
    }
}
