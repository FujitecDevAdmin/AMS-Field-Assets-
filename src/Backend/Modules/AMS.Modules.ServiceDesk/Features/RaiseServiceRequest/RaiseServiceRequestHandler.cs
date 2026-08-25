using System.Globalization;
using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceLevel.PublicApi.ServiceLevel;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;

/// <summary>
/// Raise a ticket. Catalogue: Raise a Request, which is three screens sharing
/// one form — a support ticket, a fault on an asset, and a new service.
/// </summary>
/// <remarks>
/// One table carries all three because they differ in what they ask for, not
/// in how they are worked: the same queue, the same statuses, the same clock
/// and the same conversation. The kind decides which extra questions appear
/// and, in pass three, whether an approval workflow runs.
/// </remarks>
public sealed class RaiseServiceRequestHandler(
    ServiceDeskDbContext db,
    ISlaCalculator sla,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<RaiseServiceRequestCommand, RaiseServiceRequestResponse>
{
    public async Task<Result<RaiseServiceRequestResponse>> HandleAsync(
        RaiseServiceRequestCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!RequestKind.All.Contains(request.RequestKind, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ServiceRequest.UnknownKind",
                $"Request kind must be one of {string.Join(", ", RequestKind.All)}.");
        }

        if (!RequestPriority.All.Contains(request.Priority, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ServiceRequest.UnknownPriority",
                $"Priority must be one of {string.Join(", ", RequestPriority.All)}.");
        }

        // The template is read first: it supplies the defaults for anything the
        // form left blank, and the form's own values always win. A template
        // that overrode what the requester typed would be a form that argues.
        var categoryId = request.RequestCategoryId;
        var subCategoryId = request.RequestSubCategoryId;
        var priority = request.Priority;
        int? teamId = null;
        var requiresAsset = false;

        if (request.ServiceTemplateId is { } templateId)
        {
            var template = await db.ServiceTemplates
                .SingleOrDefaultAsync(t => t.Id == templateId, ct);

            if (template is null)
            {
                return Error.NotFound("ServiceTemplate", templateId);
            }

            if (!template.IsActive)
            {
                return Error.Validation(
                    "ServiceTemplate.Retired",
                    "That template has been retired. Choose another.");
            }

            categoryId ??= template.RequestCategoryId;
            subCategoryId ??= template.RequestSubCategoryId;
            teamId = template.DefaultSupportTeamId;
            requiresAsset = template.RequiresAsset;

            if (request.RequestCategoryId is null && request.RequestSubCategoryId is null)
            {
                priority = template.DefaultPriority;
            }
        }

        var invalid = await ValidateClassificationAsync(
            request.RequestKind, categoryId, subCategoryId, ct);
        if (invalid is not null)
        {
            return invalid;
        }

        // An asset issue is about an asset. When the requester cannot find it
        // in the list they type what they have — an asset tag on a sticker, a
        // model name — and a technician reconciles it later. What is not
        // allowed is neither: a fault report naming nothing is unworkable.
        var namesAsset = request.AssetId is not null
            || !string.IsNullOrWhiteSpace(request.ManualAssetText);

        if ((request.RequestKind == RequestKind.AssetIssue || requiresAsset) && !namesAsset)
        {
            return Error.Validation(
                "ServiceRequest.AssetRequired",
                "Name the asset, or describe it if it is not on the register.");
        }

        var detailInvalid = ValidateNewServiceDetail(request);
        if (detailInvalid is not null)
        {
            return detailInvalid;
        }

        // Not the status NAMED 'Open': the status list is data a site may
        // rename. The first active status that is not a closed state is what
        // "new" means, whatever it is called.
        var status = await db.RequestStatuses
            .Where(s => s.IsActive && !s.IsClosedState)
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync(ct);

        if (status is null)
        {
            return Error.Validation(
                "RequestStatus.NoneConfigured",
                "No open ticket status is configured. Add one before raising tickets.");
        }

        var now = clock.UtcNow;

        // What "on time" means for this ticket. ServiceLevel owns the answer
        // because it depends on the branch's working week, and the calendar is
        // a property of the branch rather than of the ticket.
        //
        // Null is an ordinary answer: a site with no SLA policy configured
        // still raises tickets, and a ticket with no due date is never overdue.
        var targets = await sla.ComputeTargetsAsync(
            new SlaTargetRequest(priority, request.LocationId, now), ct);

        var ticket = new ServiceRequest
        {
            RequestNumber = await NextRequestNumberAsync(ct),
            RequestKind = request.RequestKind,
            Subject = request.Subject,
            Description = request.Description,
            Priority = priority,
            RequestStatusId = status.Id,
            RequestCategoryId = categoryId,
            RequestSubCategoryId = subCategoryId,
            ServiceTemplateId = request.ServiceTemplateId,
            AssetId = request.AssetId,
            ManualAssetText = request.ManualAssetText,
            RequestedByEmployeeId = request.RequestedByEmployeeId,
            OnBehalfOfEmployeeId = request.OnBehalfOfEmployeeId,
            LocationId = request.LocationId,
            AssignedTeamId = teamId,
            SlaPolicyId = targets?.SlaPolicyId,
            // Not the same as when it was raised. A ticket logged at ten at
            // night, or inside the branch's final minutes, starts its clock
            // when the branch next opens.
            SlaStartOnUtc = targets?.StartOnUtc ?? now,
            SlaLastCalculatedOnUtc = targets?.StartOnUtc ?? now,
            ResponseDueOnUtc = targets?.ResponseDueOnUtc,
            ResolutionDueOnUtc = targets?.ResolutionDueOnUtc,
            // CK_ServiceRequest_ScheduledHold requires the opening time
            // whenever the flag is set, so the two are written together and
            // never apart.
            IsScheduledHold = targets?.IsScheduledHold ?? false,
            NextOperationalStartUtc = targets is { IsScheduledHold: true }
                ? targets.StartOnUtc
                : null,
            ScheduleHoldReason = targets?.ScheduleHoldReason,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.ServiceRequests.Add(ticket);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        if (request.NewService is { } detail)
        {
            db.NewServiceRequestDetails.Add(new NewServiceRequestDetail
            {
                ServiceRequestId = ticket.Id,
                RequestCategoryId = categoryId!.Value,
                RequestSubCategoryId = subCategoryId!.Value,
                RequiredByDate = detail.RequiredByDate,
                Notes = detail.Notes,
            });

            foreach (var item in detail.Items)
            {
                db.NewServiceRequestItems.Add(new NewServiceRequestItem
                {
                    ServiceRequestId = ticket.Id,
                    AssetTypeId = item.AssetTypeId,
                    Quantity = item.Quantity,
                    Specification = item.Specification,
                });
            }
        }

        db.RequestHistories.Add(new RequestHistory
        {
            ServiceRequestId = ticket.Id,
            EntryKind = HistoryEntryKind.Transition,
            EntryText = $"Raised as {ticket.RequestNumber}.",
            ToStatusId = status.Id,
            OccurredOnUtc = now,
            PerformedBy = currentUser.Username,
        });

        if (targets is { IsScheduledHold: true })
        {
            // Said out loud in the timeline, because the requester's first
            // question about a ticket that is not being worked on is why.
            db.RequestHistories.Add(new RequestHistory
            {
                ServiceRequestId = ticket.Id,
                EntryKind = HistoryEntryKind.Sla,
                EntryText = targets.ScheduleHoldReason ?? "The clock has not started yet.",
                OccurredOnUtc = now,
                PerformedBy = "SLA Automation",
            });
        }

        await db.SaveChangesAsync(ct);

        return new RaiseServiceRequestResponse(
            ticket.Id, ticket.RequestNumber, ticket.RequestKind, status.StatusName);
    }

    /// <summary>
    /// The two classification columns are independent foreign keys, so nothing
    /// in the schema stops a ticket being filed under a sub-category that
    /// belongs to a different category. This does.
    /// </summary>
    private async Task<Error?> ValidateClassificationAsync(
        string requestKind,
        int? categoryId,
        int? subCategoryId,
        CancellationToken ct)
    {
        if (categoryId is null && subCategoryId is null && requestKind != RequestKind.NewService)
        {
            return null;
        }

        if (categoryId is not { } category)
        {
            return Error.Validation(
                "ServiceRequest.CategoryRequired",
                "Choose a category for the request.");
        }

        var categoryRow = await db.RequestCategories
            .Where(c => c.Id == category)
            .Select(c => new { c.CategoryType, c.IsActive })
            .SingleOrDefaultAsync(ct);

        if (categoryRow is null)
        {
            return Error.NotFound("RequestCategory", category);
        }

        if (!categoryRow.IsActive)
        {
            return Error.Validation(
                "RequestCategory.Retired",
                "That category has been retired. Choose another.");
        }

        var requiredType = RequestCategoryType.ForRequestKind(requestKind);
        if (categoryRow.CategoryType != requiredType)
        {
            return Error.Validation(
                "ServiceRequest.CategoryTypeMismatch",
                $"A {requestKind} request requires a {requiredType} category.");
        }

        if (subCategoryId is not { } subCategory)
        {
            return Error.Validation(
                "ServiceRequest.SubCategoryRequired",
                "Choose a sub-category for the request.");
        }

        var subCategoryRow = await db.RequestSubCategories
            .Where(s => s.Id == subCategory)
            .Select(s => new { s.RequestCategoryId, s.IsActive })
            .SingleOrDefaultAsync(ct);

        if (subCategoryRow is null)
        {
            return Error.NotFound("RequestSubCategory", subCategory);
        }

        if (subCategoryRow.RequestCategoryId != categoryId)
        {
            return Error.Validation(
                "ServiceRequest.SubCategoryMismatch",
                "That sub-category belongs to a different category.");
        }

        if (!subCategoryRow.IsActive)
        {
            return Error.Validation(
                "RequestSubCategory.Retired",
                "That sub-category has been retired. Choose another.");
        }

        return null;
    }

    /// <summary>
    /// The New Service questions belong to a New Service request and to nothing
    /// else — a support ticket has no joining date, and a joiner request with
    /// no answers is a form nobody filled in.
    /// </summary>
    private static Error? ValidateNewServiceDetail(RaiseServiceRequestCommand request)
    {
        if (request.RequestKind != RequestKind.NewService)
        {
            return request.NewService is null
                ? null
                : Error.Validation(
                    "ServiceRequest.NewServiceDetailNotAllowed",
                    "New service details belong to a NewService request.");
        }

        if (request.NewService is not { } detail)
        {
            return Error.Validation(
                "ServiceRequest.NewServiceDetailRequired",
                "A new service request needs its details.");
        }

        return detail.Items.Any(i => i.Quantity <= 0)
            ? Error.Validation(
                "ServiceRequest.ItemQuantity",
                "Every line must ask for at least one.")
            : null;
    }

    /// <summary>The next ticket number, from the database sequence.</summary>
    /// <remarks>
    /// A sequence and not MAX+1: two people raising at the same moment would
    /// both read the same maximum, and UX_ServiceRequest_Number would then
    /// reject one of them for no reason a user could act on.
    ///
    /// The sequence is global and never reset (R2-17). The year in the number
    /// is when the ticket was raised, not a counter that restarts — a number
    /// that repeats every January is a number people cannot quote.
    /// </remarks>
    private async Task<string> NextRequestNumberAsync(CancellationToken ct)
    {
        // A direct command, not SqlQuery<T>: EF wraps that in a subquery and
        // NEXT VALUE FOR is illegal inside one.
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT NEXT VALUE FOR [ServiceDesk].[RequestNumberSequence];";
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

        var next = Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);

        return string.Create(
            CultureInfo.InvariantCulture, $"TKT-{clock.UtcNow.Year}-{next:000000}");
    }
}
