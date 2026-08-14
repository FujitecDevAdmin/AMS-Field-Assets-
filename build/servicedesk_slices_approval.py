"""
Slice specifications for the ServiceDesk module, pass three: the approval
workflow.

Catalogue screens: Approval Workflow Setup, My Approvals, and the approval
panel on Request Detail. This is the extension block at the end of the design
script — eight tables whose governing sentence is R2-12: *an approval run is
evidence*. Nothing in it is ever deleted, and the decision rows carry NO ACTION
foreign keys to make that true rather than merely intended.

Three ideas shape every slice here:

  * A published definition is never edited. Retire it and publish a new
    VersionNumber. Editing one in place would rewrite the rules an in-flight
    approval is being judged by.

  * Approvers are resolved ONCE, at submission, and snapshotted into
    RequestApprovalParticipant with their name and address. Somebody leaving
    the company must not silently rewrite who approved what.

  * One pending step per run, one pending run per request, one decision per
    participant. All three are filtered unique indexes, so two retries collide
    in the database rather than producing two parallel chains and two lots of
    mail (rule 6).

    python build/servicedesk_slices_approval.py
"""
from slices import main

NS = "AMS.Modules.ServiceDesk"
PROJECT = "AMS.Modules.ServiceDesk"

VIEW = "Capabilities.ServiceDesk.View"
RAISE = "Capabilities.ServiceDesk.Raise"
WORKFLOW = "Capabilities.ServiceDesk.WorkflowManage"
DECIDE = "Capabilities.ServiceDesk.ApprovalDecide"
CANCEL = "Capabilities.ServiceDesk.ApprovalCancel"

SPECS = [
    # ---------------------------------------------------------- definitions
    {
        "name": "SearchApprovalWorkflows", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The approval routes and their versions. Catalogue: Approval Workflow Setup.",
        "capability": WORKFLOW,
        "verb": "Get", "route": "/approval-workflows",
        "command": [("string?", "Name"), ("bool", "PublishedOnly"), ("bool", "ActiveOnly"),
                    ("int?", "ServiceTemplateId")],
        "request": [("string?", "Name"), ("bool?", "PublishedOnly"), ("bool?", "ActiveOnly"),
                    ("int?", "ServiceTemplateId")],
        "response": [("IReadOnlyList<SearchApprovalWorkflowsResponse.Row>", "Rows")],
        "responseSummary": "Every version of every route, newest version first.",
        "responseDocs": {"Rows": "The routes, each with its stages and their approver rules."},
        "rules": ["RuleFor(x => x.Name).MaximumLength(150);"],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim()",
                    "request.PublishedOnly ?? false", "request.ActiveOnly ?? false",
                    "request.ServiceTemplateId"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchApprovalWorkflowsRequest request,\n",
    },
    {
        "name": "CreateApprovalWorkflow", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Draft a new approval route, or a new version of one. "
                   "Catalogue: Approval Workflow Setup.",
        "capability": WORKFLOW,
        "verb": "Post", "route": "/approval-workflows",
        "command": [("string", "WorkflowName"), ("string?", "Description"),
                    ("int?", "ServiceTemplateId"), ("int?", "LocationId"), ("string?", "Priority"),
                    ("bool", "IsDefault"),
                    ("IReadOnlyList<CreateApprovalWorkflowCommand.Stage>", "Stages")],
        "request": [("string", "WorkflowName"), ("string?", "Description"),
                    ("int?", "ServiceTemplateId"), ("int?", "LocationId"), ("string?", "Priority"),
                    ("bool?", "IsDefault"),
                    ("IReadOnlyList<CreateApprovalWorkflowRequest.Stage>", "Stages")],
        "response": [("int", "Id"), ("string", "WorkflowName"), ("int", "VersionNumber"),
                     ("int", "StageCount")],
        "responseSummary": "The draft. It approves nothing until it is published.",
        "responseDocs": {
            "Id": "The definition.",
            "WorkflowName": "The route's name, shared by every version of it.",
            "VersionNumber": "One higher than the highest version of that name.",
            "StageCount": "How many levels it has.",
        },
        "rules": [
            "RuleFor(x => x.WorkflowName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.Description).MaximumLength(500);",
            "RuleFor(x => x.Priority).MaximumLength(20);",
            "RuleFor(x => x.Stages).NotEmpty();",
        ],
        "mapArgs": ["request.WorkflowName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()",
                    "request.ServiceTemplateId", "request.LocationId",
                    "string.IsNullOrWhiteSpace(request.Priority) ? null : request.Priority.Trim()",
                    "request.IsDefault ?? false",
                    "[.. request.Stages.Select((s, index) => new CreateApprovalWorkflowCommand.Stage(\n"
                    "                index + 1,\n"
                    "                s.StageName.Trim(),\n"
                    "                s.ApprovalMode.Trim(),\n"
                    "                s.DueAfterMinutes,\n"
                    "                s.ReminderAfterMinutes,\n"
                    "                s.ReminderRepeatMinutes,\n"
                    "                s.EscalateAfterMinutes,\n"
                    "                s.AllowDelegation ?? false,\n"
                    "                [.. s.Rules.Select(r => new CreateApprovalWorkflowCommand.Rule(\n"
                    "                    r.ResolverType.Trim(),\n"
                    "                    r.ResolverUserId,\n"
                    "                    r.ResolverRoleId,\n"
                    "                    string.IsNullOrWhiteSpace(r.ResolverCapabilityName) ? null : r.ResolverCapabilityName.Trim(),\n"
                    "                    string.IsNullOrWhiteSpace(r.ResolverEmail) ? null : r.ResolverEmail.Trim(),\n"
                    "                    string.IsNullOrWhiteSpace(r.DisplayName) ? null : r.DisplayName.Trim(),\n"
                    "                    r.IsRequired ?? true))]))]"],
        "mapCall": "request",
        "bind": "                CreateApprovalWorkflowRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "PublishApprovalWorkflow", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Publish a draft route, or retire a published one. "
                   "Catalogue: Approval Workflow Setup.",
        "capability": WORKFLOW,
        "verb": "Post", "route": "/approval-workflows/{id:int}/publication",
        "command": [("int", "Id"), ("bool", "IsPublished"), ("bool", "IsActive"),
                    ("DateTime?", "EffectiveFromUtc"), ("DateTime?", "EffectiveToUtc")],
        "request": [("bool?", "IsPublished"), ("bool?", "IsActive"),
                    ("DateTime?", "EffectiveFromUtc"), ("DateTime?", "EffectiveToUtc")],
        "response": [("int", "Id"), ("string", "WorkflowName"), ("int", "VersionNumber"),
                     ("bool", "IsPublished"), ("bool", "IsActive")],
        "responseSummary": "Where the definition now stands.",
        "responseDocs": {
            "Id": "The definition.",
            "WorkflowName": "The route.",
            "VersionNumber": "Which version this is.",
            "IsPublished": "Whether submissions may pick it up.",
            "IsActive": "Whether it is in use at all. Retiring is how a route is replaced.",
        },
        "rules": [],
        "mapArgs": ["id", "request.IsPublished ?? true", "request.IsActive ?? true",
                    "request.EffectiveFromUtc", "request.EffectiveToUtc"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                PublishApprovalWorkflowRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # -------------------------------------------------------------- runtime
    {
        "name": "SubmitForApproval", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Send a new service request for approval. Catalogue: Submit for Approval "
                   "on Request Detail.",
        "capability": RAISE,
        "verb": "Post", "route": "/requests/{id:int}/approval",
        "command": [("int", "Id"), ("int?", "ApprovalWorkflowId")],
        "request": [("int?", "ApprovalWorkflowId")],
        "response": [("long", "Id"), ("int", "ServiceRequestId"), ("string", "WorkflowName"),
                     ("int", "WorkflowVersion"), ("string", "Status"), ("int?", "CurrentStageNumber"),
                     ("int", "ApproverCount")],
        "responseSummary": "The approval run, waiting on its first level.",
        "responseDocs": {
            "Id": "The run.",
            "ServiceRequestId": "The request being approved.",
            "WorkflowName": "Copied onto the run, so the audit reads without a join.",
            "WorkflowVersion": "Which version is judging it. Fixed for the life of the run.",
            "Status": "Always Pending. A run that approved itself would not be a run.",
            "CurrentStageNumber": "The level now waiting.",
            "ApproverCount": "How many people were resolved into the first level.",
        },
        "rules": [],
        "mapArgs": ["id", "request.ApprovalWorkflowId"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                SubmitForApprovalRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SearchMyApprovals", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What is waiting on me. Catalogue: My Approvals.",
        "capability": DECIDE,
        "verb": "Get", "route": "/approvals/mine",
        "command": [("int", "UserId"), ("bool", "PendingOnly"), ("int", "Skip"), ("int", "Take")],
        "request": [("bool?", "PendingOnly"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchMyApprovalsResponse.Row>", "Rows"), ("int", "TotalCount"),
                     ("int", "OverdueCount")],
        "responseSummary": "My approvals, most overdue first.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Approvals matching the filter.",
            "OverdueCount": "How many are past their due time, over the whole filter.",
        },
        "rules": [
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["userId", "request.PendingOnly ?? true", "request.Skip ?? 0",
                    "request.Take ?? 50"],
        "mapCall": "request, currentUser.Id",
        "mapExtra": [("int", "userId")],
        "bind": "                [AsParameters] SearchMyApprovalsRequest request,\n"
                "                ICurrentUser currentUser,\n",
    },
    {
        "name": "GetRequestApproval", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The approval run on one request, with every level and every decision. "
                   "Catalogue: the approval panel on Request Detail.",
        "capability": VIEW,
        "verb": "Get", "route": "/requests/{id:int}/approval",
        "command": [("int", "Id")],
        "request": [],
        "response": [("long", "Id"), ("int", "ServiceRequestId"), ("string", "WorkflowName"),
                     ("int", "WorkflowVersion"), ("string", "Status"), ("int?", "CurrentStageNumber"),
                     ("int", "SubmittedByUserId"), ("DateTime", "SubmittedOnUtc"),
                     ("DateTime?", "CompletedOnUtc"), ("DateTime?", "CancelledOnUtc"),
                     ("string?", "CancellationReason"),
                     ("IReadOnlyList<GetRequestApprovalResponse.Step>", "Steps")],
        "responseSummary": "The run as the panel draws it.",
        "responseDocs": {
            "Id": "The run.",
            "ServiceRequestId": "The request.",
            "WorkflowName": "Which route, as it was named when this run started.",
            "WorkflowVersion": "Which version.",
            "Status": "Pending, Approved, Rejected or Cancelled.",
            "CurrentStageNumber": "The level now waiting, if the run is still going.",
            "SubmittedByUserId": "Who sent it.",
            "SubmittedOnUtc": "When.",
            "CompletedOnUtc": "When it finished, whichever way.",
            "CancelledOnUtc": "When it was called off.",
            "CancellationReason": "Why. Required, because a run that stopped for no stated reason is not evidence.",
            "Steps": "Every level, in order, each with its approvers and their decisions.",
        },
        "rules": [],
        "mapArgs": ["id"],
        "mapCall": "new GetRequestApprovalRequest(), id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "DecideApproval", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Approve or reject the level waiting on me. Catalogue: My Approvals.",
        "capability": DECIDE,
        "verb": "Post", "route": "/approvals/participants/{participantId:long}/decision",
        "command": [("long", "ParticipantId"), ("Guid", "ClientDecisionId"), ("bool", "Approved"),
                    ("string?", "Remarks"), ("string", "Source")],
        "request": [("Guid?", "ClientDecisionId"), ("bool", "Approved"), ("string?", "Remarks"),
                    ("string?", "Source")],
        "response": [("long", "ParticipantId"), ("string", "Decision"), ("string", "StepStatus"),
                     ("string", "InstanceStatus"), ("int?", "CurrentStageNumber"),
                     ("bool", "WasAlreadyDecided")],
        "responseSummary": "What the decision did to the level and to the run.",
        "responseDocs": {
            "ParticipantId": "The approver line that was decided.",
            "Decision": "Approved or Rejected.",
            "StepStatus": "Where the level stands now.",
            "InstanceStatus": "Where the whole run stands now.",
            "CurrentStageNumber": "The next level, if the run moved on.",
            "WasAlreadyDecided": "True when this call replayed a decision already recorded "
                                 "under the same ClientDecisionId. The answer is the same one, "
                                 "not a second decision.",
        },
        "rules": [
            "RuleFor(x => x.Remarks).MaximumLength(1000);",
            "RuleFor(x => x.Source).MaximumLength(20);",
        ],
        "mapArgs": ["participantId", "request.ClientDecisionId ?? Guid.NewGuid()", "request.Approved",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()",
                    "string.IsNullOrWhiteSpace(request.Source) ? DecisionSource.Application : request.Source.Trim()"],
        "mapCall": "request, participantId",
        "mapExtra": [("long", "participantId")],
        "bind": "                long participantId,\n                DecideApprovalRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "CancelApproval", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Call off an approval run, with a reason. Catalogue: the approval panel on "
                   "Request Detail.",
        "capability": CANCEL,
        "verb": "Post", "route": "/requests/{id:int}/approval/cancellation",
        "command": [("int", "Id"), ("string", "Reason")],
        "request": [("string", "Reason")],
        "response": [("long", "Id"), ("int", "ServiceRequestId"), ("string", "Status"),
                     ("DateTime", "CancelledOnUtc")],
        "responseSummary": "The run, stopped.",
        "responseDocs": {
            "Id": "The run.",
            "ServiceRequestId": "The request it was for.",
            "Status": "Always Cancelled.",
            "CancelledOnUtc": "When it was called off.",
        },
        "rules": ["RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);"],
        "mapArgs": ["id", "request.Reason.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                CancelApprovalRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
