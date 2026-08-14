"""
Slice specifications for the ServiceDesk module, pass two: the tickets.

Catalogue screens: Raise a Request, My Requests, the technician Queue, and
Request Detail with its conversation. Pass one built the master data a ticket
refers to; pass three builds the approval workflow a New Service request runs
through.

Two things shape every slice here:

  * One table carries three kinds of request. SupportTicket, AssetIssue and
    NewService differ in what they ask for, not in how they are worked, so
    they share a queue, a status list, a history and an SLA clock.

  * The queue sorts overdue first, then nearest due. IX_ServiceRequest_SlaQueue
    exists for exactly that ORDER BY, and IsSlaOverdue is a persisted column
    rather than a computed one so the index can be used.

    python build/servicedesk_slices_tickets.py
"""
from slices import main

NS = "AMS.Modules.ServiceDesk"
PROJECT = "AMS.Modules.ServiceDesk"

RAISE = "Capabilities.ServiceDesk.Raise"
VIEW = "Capabilities.ServiceDesk.View"
MANAGE = "Capabilities.ServiceDesk.Manage"
ASSIGN = "Capabilities.ServiceDesk.Assign"
NOTE = "Capabilities.ServiceDesk.Note"
EMAIL = "Capabilities.ServiceDesk.Email"
ATTACH = "Capabilities.ServiceDesk.Attach"

SPECS = [
    # ------------------------------------------------------------- raising
    {
        "name": "RaiseServiceRequest", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Raise a ticket, report a fault on an asset, or ask for a new service. "
                   "Catalogue: Raise a Request.",
        "capability": RAISE,
        "verb": "Post", "route": "/requests",
        "command": [("string", "RequestKind"), ("string", "Subject"), ("string?", "Description"),
                    ("string", "Priority"), ("int?", "RequestCategoryId"),
                    ("int?", "RequestSubCategoryId"), ("int?", "ServiceTemplateId"),
                    ("int?", "AssetId"), ("string?", "ManualAssetText"),
                    ("int", "RequestedByEmployeeId"), ("int?", "OnBehalfOfEmployeeId"),
                    ("int?", "LocationId"),
                    ("RaiseServiceRequestCommand.NewServiceDetail?", "NewService")],
        "request": [("string", "RequestKind"), ("string", "Subject"), ("string?", "Description"),
                    ("string?", "Priority"), ("int?", "RequestCategoryId"),
                    ("int?", "RequestSubCategoryId"), ("int?", "ServiceTemplateId"),
                    ("int?", "AssetId"), ("string?", "ManualAssetText"),
                    ("int", "RequestedByEmployeeId"), ("int?", "OnBehalfOfEmployeeId"),
                    ("int?", "LocationId"),
                    ("RaiseServiceRequestRequest.NewServiceDetail?", "NewService")],
        "response": [("int", "Id"), ("string", "RequestNumber"), ("string", "RequestKind"),
                     ("string", "Status")],
        "responseSummary": "The ticket, open and numbered.",
        "responseDocs": {
            "Id": "The ticket.",
            "RequestNumber": "TKT-2026-000123. Drawn from a sequence, never reset (R2-17).",
            "RequestKind": "SupportTicket, AssetIssue or NewService.",
            "Status": "Always Open. Assignment and the clock are separate decisions.",
        },
        "rules": [
            "RuleFor(x => x.RequestKind).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);",
            "RuleFor(x => x.Description).MaximumLength(4000);",
            "RuleFor(x => x.Priority).MaximumLength(20);",
            "RuleFor(x => x.ManualAssetText).MaximumLength(200);",
            "RuleFor(x => x.RequestedByEmployeeId).GreaterThan(0);",
        ],
        "mapArgs": ["request.RequestKind.Trim()", "request.Subject.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()",
                    "string.IsNullOrWhiteSpace(request.Priority) ? RequestPriority.Medium : request.Priority.Trim()",
                    "request.RequestCategoryId", "request.RequestSubCategoryId",
                    "request.ServiceTemplateId", "request.AssetId",
                    "string.IsNullOrWhiteSpace(request.ManualAssetText) ? null : request.ManualAssetText.Trim()",
                    "request.RequestedByEmployeeId", "request.OnBehalfOfEmployeeId",
                    "request.LocationId",
                    "request.NewService is null ? null : new RaiseServiceRequestCommand.NewServiceDetail(\n"
                    "                request.NewService.NeedsEmail,\n"
                    "                request.NewService.NeedsErp,\n"
                    "                request.NewService.NeedsDms,\n"
                    "                request.NewService.NeedsVpn,\n"
                    "                request.NewService.RequiredByDate,\n"
                    "                string.IsNullOrWhiteSpace(request.NewService.Notes) ? null : request.NewService.Notes.Trim(),\n"
                    "                [.. request.NewService.Items.Select(i => new RaiseServiceRequestCommand.NewServiceItem(\n"
                    "                    i.AssetTypeId,\n"
                    "                    i.Quantity,\n"
                    "                    string.IsNullOrWhiteSpace(i.Specification) ? null : i.Specification.Trim()))])"],
        "mapCall": "request",
        "bind": "                RaiseServiceRequestRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # ------------------------------------------------------------- reading
    {
        "name": "SearchMyRequests", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What I have asked for and where it has got to. Catalogue: My Requests.",
        "capability": RAISE,
        "verb": "Get", "route": "/requests/mine",
        "command": [("int", "EmployeeId"), ("bool", "OpenOnly"), ("int", "Skip"), ("int", "Take")],
        "request": [("bool?", "OpenOnly"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchMyRequestsResponse.Row>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "My tickets, newest first.",
        "responseDocs": {"Rows": "The page.", "TotalCount": "Tickets matching the filter."},
        "rules": [
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["employeeId", "request.OpenOnly ?? false", "request.Skip ?? 0",
                    "request.Take ?? 50"],
        "mapCall": "request, currentUser.EmployeeId ?? 0",
        "mapExtra": [("int", "employeeId")],
        # The employee is taken from the token, never from the query string:
        # a screen called "My Requests" that accepts whose is not that screen.
        "bind": "                [AsParameters] SearchMyRequestsRequest request,\n"
                "                ICurrentUser currentUser,\n",
    },
    {
        "name": "SearchRequestQueue", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The technician queue: overdue first, then nearest due. "
                   "Catalogue: Service Request Queue.",
        "capability": VIEW,
        "verb": "Get", "route": "/requests",
        "command": [("string?", "RequestKind"), ("int?", "RequestStatusId"), ("string?", "Priority"),
                    ("int?", "AssignedToUserId"), ("int?", "AssignedTeamId"), ("int?", "LocationId"),
                    ("bool", "Unassigned"), ("bool", "OverdueOnly"), ("bool", "OpenOnly"),
                    ("string?", "Search"), ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "RequestKind"), ("int?", "RequestStatusId"), ("string?", "Priority"),
                    ("int?", "AssignedToUserId"), ("int?", "AssignedTeamId"), ("int?", "LocationId"),
                    ("bool?", "Unassigned"), ("bool?", "OverdueOnly"), ("bool?", "OpenOnly"),
                    ("string?", "Search"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchRequestQueueResponse.Row>", "Rows"),
                     ("int", "TotalCount"), ("int", "OverdueCount")],
        "responseSummary": "One page of the queue, worst first.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Tickets matching the filter.",
            "OverdueCount": "How many of those have blown their SLA. The number the "
                            "screen puts in red, counted over the whole filter and not "
                            "just the page.",
        },
        "rules": [
            "RuleFor(x => x.RequestKind).MaximumLength(20);",
            "RuleFor(x => x.Priority).MaximumLength(20);",
            "RuleFor(x => x.Search).MaximumLength(300);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.RequestKind) ? null : request.RequestKind.Trim()",
                    "request.RequestStatusId",
                    "string.IsNullOrWhiteSpace(request.Priority) ? null : request.Priority.Trim()",
                    "request.AssignedToUserId", "request.AssignedTeamId", "request.LocationId",
                    "request.Unassigned ?? false", "request.OverdueOnly ?? false",
                    "request.OpenOnly ?? true",
                    "string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()",
                    "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchRequestQueueRequest request,\n",
    },
    {
        "name": "GetServiceRequest", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "One ticket with its conversation, its files and its clock. "
                   "Catalogue: Request Detail.",
        "capability": VIEW,
        "verb": "Get", "route": "/requests/{id:int}",
        "command": [("int", "Id"), ("bool", "IncludeInternal")],
        "request": [("bool?", "IncludeInternal")],
        "response": [("int", "Id"), ("string", "RequestNumber"), ("string", "RequestKind"),
                     ("string", "Subject"), ("string?", "Description"), ("string", "Priority"),
                     ("int", "RequestStatusId"), ("string", "StatusName"), ("bool", "IsClosedState"),
                     ("int?", "RequestCategoryId"), ("string?", "CategoryName"),
                     ("int?", "RequestSubCategoryId"), ("string?", "SubCategoryName"),
                     ("int?", "AssetId"), ("string?", "ManualAssetText"),
                     ("int", "RequestedByEmployeeId"), ("int?", "OnBehalfOfEmployeeId"),
                     ("int?", "LocationId"), ("int?", "AssignedToUserId"),
                     ("int?", "AssignedTeamId"), ("string?", "AssignedTeamName"),
                     ("DateTime?", "AssignedOnUtc"), ("DateTime?", "ResolvedOnUtc"),
                     ("DateTime?", "ClosedOnUtc"), ("string?", "Resolution"),
                     ("DateTime?", "ResponseDueOnUtc"), ("DateTime?", "ResolutionDueOnUtc"),
                     ("DateTime?", "FirstResponseOnUtc"), ("bool", "IsSlaPaused"),
                     ("bool", "IsSlaOverdue"), ("int", "ResolutionConsumedMinutes"),
                     ("DateTime", "CreatedOnUtc"),
                     ("GetServiceRequestResponse.NewServiceDetail?", "NewService"),
                     ("IReadOnlyList<GetServiceRequestResponse.HistoryEntry>", "History"),
                     ("IReadOnlyList<GetServiceRequestResponse.Attachment>", "Attachments")],
        "responseSummary": "Everything the detail screen draws.",
        "responseDocs": {
            "Id": "The ticket.",
            "RequestNumber": "What the requester quotes.",
            "RequestKind": "SupportTicket, AssetIssue or NewService.",
            "Subject": "The one-line summary.",
            "Description": "What the requester wrote.",
            "Priority": "Low, Medium, High or Critical.",
            "RequestStatusId": "Where it is.",
            "StatusName": "Resolved once here so the screen need not hold the status list.",
            "IsClosedState": "Whether the ticket is finished. What every open-queue filter tests.",
            "RequestCategoryId": "Classification, if any.",
            "CategoryName": "Resolved for display.",
            "RequestSubCategoryId": "Finer classification, if any.",
            "SubCategoryName": "Resolved for display.",
            "AssetId": "Assets.Asset, id only (rule 2).",
            "ManualAssetText": "What the requester typed when the asset is not on record.",
            "RequestedByEmployeeId": "Who asked.",
            "OnBehalfOfEmployeeId": "Who it is for, when somebody raised it for them.",
            "LocationId": "The site.",
            "AssignedToUserId": "The technician, if one holds it.",
            "AssignedTeamId": "The team, if it sits with a team rather than a person.",
            "AssignedTeamName": "Resolved for display.",
            "AssignedOnUtc": "When it was last handed to somebody.",
            "ResolvedOnUtc": "When a fix was recorded.",
            "ClosedOnUtc": "When it was closed.",
            "Resolution": "What was done.",
            "ResponseDueOnUtc": "When somebody must have replied by.",
            "ResolutionDueOnUtc": "When it must be fixed by.",
            "FirstResponseOnUtc": "When somebody first did, stamped once and never again.",
            "IsSlaPaused": "Whether the clock is frozen by the current status.",
            "IsSlaOverdue": "Persisted, not derived, so the queue can sort on it.",
            "ResolutionConsumedMinutes": "Operational minutes spent, not wall clock.",
            "CreatedOnUtc": "When it was raised.",
            "NewService": "The joiner/kit detail, on a NewService request only.",
            "History": "Conversations and History as one chronological list.",
            "Attachments": "Files on the ticket, including those that arrived by e-mail.",
        },
        "rules": [],
        "mapArgs": ["id", "request.IncludeInternal ?? false"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n"
                "                [AsParameters] GetServiceRequestRequest request,\n",
        "otherStatuses": ["Status404NotFound"],
    },

    # ------------------------------------------------------------- working
    {
        "name": "AssignServiceRequest", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Hand a ticket to a technician, a team, or both. Catalogue: Assign.",
        "capability": ASSIGN,
        "verb": "Post", "route": "/requests/{id:int}/assignment",
        "command": [("int", "Id"), ("int?", "AssignedToUserId"), ("int?", "AssignedTeamId"),
                    ("string?", "Remarks")],
        "request": [("int?", "AssignedToUserId"), ("int?", "AssignedTeamId"), ("string?", "Remarks")],
        "response": [("int", "Id"), ("int?", "AssignedToUserId"), ("int?", "AssignedTeamId"),
                     ("int", "RequestStatusId"), ("string", "StatusName")],
        "responseSummary": "Who holds it now.",
        "responseDocs": {
            "Id": "The ticket.",
            "AssignedToUserId": "The technician, or null when it sits with a team.",
            "AssignedTeamId": "The team.",
            "RequestStatusId": "Where it is: assigning an Open ticket moves it to Assigned.",
            "StatusName": "Resolved for display.",
        },
        "rules": ["RuleFor(x => x.Remarks).MaximumLength(500);"],
        "mapArgs": ["id", "request.AssignedToUserId", "request.AssignedTeamId",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                AssignServiceRequestRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "ChangeRequestStatus", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Move a ticket: start it, hold it, resolve it, close it, reopen it. "
                   "Catalogue: the status bar on Request Detail.",
        "capability": MANAGE,
        "verb": "Post", "route": "/requests/{id:int}/status",
        "command": [("int", "Id"), ("int", "RequestStatusId"), ("string?", "Resolution"),
                    ("string?", "Remarks")],
        "request": [("int", "RequestStatusId"), ("string?", "Resolution"), ("string?", "Remarks")],
        "response": [("int", "Id"), ("int", "RequestStatusId"), ("string", "StatusName"),
                     ("bool", "IsClosedState"), ("bool", "IsSlaPaused"),
                     ("int", "ResolutionConsumedMinutes")],
        "responseSummary": "Where the ticket is now, and what its clock did.",
        "responseDocs": {
            "Id": "The ticket.",
            "RequestStatusId": "Where it is now.",
            "StatusName": "Resolved for display.",
            "IsClosedState": "Whether it is finished.",
            "IsSlaPaused": "Whether the new status freezes the clock.",
            "ResolutionConsumedMinutes": "Minutes charged so far, updated by this move.",
        },
        "rules": [
            "RuleFor(x => x.RequestStatusId).GreaterThan(0);",
            "RuleFor(x => x.Resolution).MaximumLength(4000);",
            "RuleFor(x => x.Remarks).MaximumLength(500);",
        ],
        "mapArgs": ["id", "request.RequestStatusId",
                    "string.IsNullOrWhiteSpace(request.Resolution) ? null : request.Resolution.Trim()",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                ChangeRequestStatusRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "AddRequestNote", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add a note to the conversation, public or internal. "
                   "Catalogue: Conversations and History.",
        "capability": NOTE,
        "verb": "Post", "route": "/requests/{id:int}/notes",
        "command": [("int", "Id"), ("string", "Note"), ("bool", "IsInternal")],
        "request": [("string", "Note"), ("bool?", "IsInternal")],
        "response": [("long", "Id"), ("int", "ServiceRequestId"), ("bool", "IsInternal"),
                     ("DateTime", "OccurredOnUtc")],
        "responseSummary": "The entry, as it now sits in the timeline.",
        "responseDocs": {
            "Id": "The history entry.",
            "ServiceRequestId": "The ticket.",
            "IsInternal": "Hidden from the requester. Never hidden from audit.",
            "OccurredOnUtc": "When it was written.",
        },
        "rules": ["RuleFor(x => x.Note).NotEmpty().MaximumLength(4000);"],
        "mapArgs": ["id", "request.Note.Trim()", "request.IsInternal ?? false"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                AddRequestNoteRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "SendRequestEmail", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Send e-mail from a ticket. Catalogue: Send e-mail on Request Detail.",
        "capability": EMAIL,
        "verb": "Post", "route": "/requests/{id:int}/emails",
        "command": [("int", "Id"), ("string", "ToAddresses"), ("string?", "CcAddresses"),
                    ("string", "Subject"), ("string", "Body"), ("bool", "IsHtml")],
        "request": [("string", "ToAddresses"), ("string?", "CcAddresses"), ("string", "Subject"),
                    ("string", "Body"), ("bool?", "IsHtml")],
        "response": [("int", "Id"), ("int", "ServiceRequestId"), ("string", "Status")],
        "responseSummary": "The message, queued.",
        "responseDocs": {
            "Id": "The e-mail row.",
            "ServiceRequestId": "The ticket it belongs to.",
            "Status": "Always Queued. Delivery is the Notifications module's job, and "
                      "SMTP acceptance is not inbox placement.",
        },
        "rules": [
            "RuleFor(x => x.ToAddresses).NotEmpty().MaximumLength(1000);",
            "RuleFor(x => x.CcAddresses).MaximumLength(1000);",
            "RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);",
            "RuleFor(x => x.Body).NotEmpty();",
        ],
        "mapArgs": ["id", "request.ToAddresses.Trim()",
                    "string.IsNullOrWhiteSpace(request.CcAddresses) ? null : request.CcAddresses.Trim()",
                    "request.Subject.Trim()", "request.Body", "request.IsHtml ?? true"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                SendRequestEmailRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "AddRequestAttachment", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Record a file against a ticket. Catalogue: Attachments.",
        "capability": ATTACH,
        "verb": "Post", "route": "/requests/{id:int}/attachments",
        "command": [("int", "Id"), ("string", "AttachmentType"), ("string", "FilePath"),
                    ("string?", "FileName"), ("string?", "ContentType"), ("long?", "SizeBytes")],
        "request": [("string?", "AttachmentType"), ("string", "FilePath"), ("string?", "FileName"),
                    ("string?", "ContentType"), ("long?", "SizeBytes")],
        "response": [("int", "Id"), ("int", "ServiceRequestId"), ("string", "AttachmentType"),
                     ("string?", "FileName")],
        "responseSummary": "The file, as listed on the ticket.",
        "responseDocs": {
            "Id": "The attachment row.",
            "ServiceRequestId": "The ticket.",
            "AttachmentType": "Requester, Resolution or Email.",
            "FileName": "What to show; FilePath is where it actually lives.",
        },
        "rules": [
            "RuleFor(x => x.AttachmentType).MaximumLength(30);",
            "RuleFor(x => x.FilePath).NotEmpty().MaximumLength(400);",
            "RuleFor(x => x.FileName).MaximumLength(260);",
            "RuleFor(x => x.ContentType).MaximumLength(120);",
            "RuleFor(x => x.SizeBytes).GreaterThan(0).When(x => x.SizeBytes.HasValue);",
        ],
        "mapArgs": ["id",
                    "string.IsNullOrWhiteSpace(request.AttachmentType) ? AttachmentKind.Requester : request.AttachmentType.Trim()",
                    "request.FilePath.Trim()",
                    "string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim()",
                    "string.IsNullOrWhiteSpace(request.ContentType) ? null : request.ContentType.Trim()",
                    "request.SizeBytes"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                AddRequestAttachmentRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound"],
    },
]

if __name__ == "__main__":
    main(SPECS)
