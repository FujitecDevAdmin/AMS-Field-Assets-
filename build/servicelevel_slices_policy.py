"""
Slice specifications for the ServiceLevel module, pass two: SLA policies and
escalation.

Catalogue screens: SLA Policy Setup and its escalation ladder. Pass one built
the calendar these targets are measured in; this is the half that answers "is
this ticket late".

Two things shape the slices:

  * One live policy per priority. UX_SlaPolicy_ActivePriority is a filtered
    unique index, so two active "High" policies collide in the database rather
    than leaving a ticket to get whichever the query ordered first.

  * Targets are stored in MINUTES. The handbook's days/hours/minutes editor is
    a presentation concern, and three columns to store one duration is three
    chances to disagree.

    python build/servicelevel_slices_policy.py
"""
from slices import main

NS = "AMS.Modules.ServiceLevel"
PROJECT = "AMS.Modules.ServiceLevel"

SLA = "Capabilities.ServiceLevel.SlaManage"

SPECS = [
    {
        "name": "SearchSlaPolicies", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The SLA policies and their escalation ladders. Catalogue: SLA Policy Setup.",
        "capability": SLA,
        "verb": "Get", "route": "/sla-policies",
        "command": [("string?", "Priority"), ("bool", "ActiveOnly")],
        "request": [("string?", "Priority"), ("bool?", "ActiveOnly")],
        "response": [("IReadOnlyList<SearchSlaPoliciesResponse.Row>", "Rows")],
        "responseSummary": "The policies, most urgent priority first.",
        "responseDocs": {"Rows": "Each policy with the escalations configured against it."},
        "rules": ["RuleFor(x => x.Priority).MaximumLength(20);"],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Priority) ? null : request.Priority.Trim()",
                    "request.ActiveOnly ?? false"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchSlaPoliciesRequest request,\n",
    },
    {
        "name": "CreateSlaPolicy", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add an SLA policy. Catalogue: SLA Policy Setup.",
        "capability": SLA,
        "verb": "Post", "route": "/sla-policies",
        "command": [("string", "PolicyName"), ("string?", "Description"), ("string", "Priority"),
                    ("int", "ResponseTargetMinutes"), ("int", "ResolutionTargetMinutes"),
                    ("bool", "RespectOperationalHours"), ("bool", "RespectHolidays"),
                    ("bool", "RespectWeekends"), ("int", "NearDueWarningMinutes")],
        "request": [("string", "PolicyName"), ("string?", "Description"), ("string", "Priority"),
                    ("int", "ResponseTargetMinutes"), ("int", "ResolutionTargetMinutes"),
                    ("bool?", "RespectOperationalHours"), ("bool?", "RespectHolidays"),
                    ("bool?", "RespectWeekends"), ("int?", "NearDueWarningMinutes")],
        "response": [("int", "Id"), ("string", "PolicyName"), ("string", "Priority")],
        "responseSummary": "The policy, live for its priority.",
        "responseDocs": {
            "Id": "The policy.",
            "PolicyName": "What it is called.",
            "Priority": "The priority it covers. Only one active policy may.",
        },
        "rules": [
            "RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.Description).MaximumLength(500);",
            "RuleFor(x => x.Priority).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.ResponseTargetMinutes).GreaterThan(0);",
            "RuleFor(x => x.ResolutionTargetMinutes).GreaterThan(0);",
            "RuleFor(x => x.NearDueWarningMinutes).GreaterThanOrEqualTo(0)"
            ".When(x => x.NearDueWarningMinutes.HasValue);",
        ],
        "mapArgs": ["request.PolicyName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()",
                    "request.Priority.Trim()",
                    "request.ResponseTargetMinutes", "request.ResolutionTargetMinutes",
                    "request.RespectOperationalHours ?? true", "request.RespectHolidays ?? true",
                    "request.RespectWeekends ?? true", "request.NearDueWarningMinutes ?? 30"],
        "mapCall": "request",
        "bind": "                CreateSlaPolicyRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateSlaPolicy", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit an SLA policy or retire it. Catalogue: SLA Policy Setup.",
        "capability": SLA,
        "verb": "Put", "route": "/sla-policies/{id:int}",
        "command": [("int", "Id"), ("string", "PolicyName"), ("string?", "Description"),
                    ("int", "ResponseTargetMinutes"), ("int", "ResolutionTargetMinutes"),
                    ("bool", "RespectOperationalHours"), ("bool", "RespectHolidays"),
                    ("bool", "RespectWeekends"), ("int", "NearDueWarningMinutes"),
                    ("bool", "IsActive")],
        "request": [("string", "PolicyName"), ("string?", "Description"),
                    ("int", "ResponseTargetMinutes"), ("int", "ResolutionTargetMinutes"),
                    ("bool?", "RespectOperationalHours"), ("bool?", "RespectHolidays"),
                    ("bool?", "RespectWeekends"), ("int?", "NearDueWarningMinutes"),
                    ("bool?", "IsActive")],
        "response": [("int", "Id"), ("string", "PolicyName"), ("bool", "IsActive")],
        "responseSummary": "The policy as it now stands.",
        "responseDocs": {
            "Id": "The policy.",
            "PolicyName": "What it is called.",
            "IsActive": "Whether tickets of that priority are judged by it.",
        },
        "rules": [
            "RuleFor(x => x.PolicyName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.Description).MaximumLength(500);",
            "RuleFor(x => x.ResponseTargetMinutes).GreaterThan(0);",
            "RuleFor(x => x.ResolutionTargetMinutes).GreaterThan(0);",
            "RuleFor(x => x.NearDueWarningMinutes).GreaterThanOrEqualTo(0)"
            ".When(x => x.NearDueWarningMinutes.HasValue);",
        ],
        "mapArgs": ["id", "request.PolicyName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()",
                    "request.ResponseTargetMinutes", "request.ResolutionTargetMinutes",
                    "request.RespectOperationalHours ?? true", "request.RespectHolidays ?? true",
                    "request.RespectWeekends ?? true", "request.NearDueWarningMinutes ?? 30",
                    "request.IsActive ?? true"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateSlaPolicyRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SetSlaEscalations", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Set a policy's escalation ladder, all of it at once. "
                   "Catalogue: SLA Policy Setup.",
        "capability": SLA,
        "verb": "Put", "route": "/sla-policies/{id:int}/escalations",
        "command": [("int", "Id"),
                    ("IReadOnlyList<SetSlaEscalationsCommand.Rung>", "Levels")],
        "request": [("IReadOnlyList<SetSlaEscalationsRequest.Rung>", "Levels")],
        "response": [("int", "SlaPolicyId"), ("int", "ResponseLevelCount"),
                     ("int", "ResolutionLevelCount")],
        "responseSummary": "The ladder as it now stands.",
        "responseDocs": {
            "SlaPolicyId": "The policy.",
            "ResponseLevelCount": "How many levels chase a missed response.",
            "ResolutionLevelCount": "How many chase a missed resolution.",
        },
        "rules": ["RuleFor(x => x.Levels).NotNull();"],
        "mapArgs": ["id",
                    "[.. request.Levels.Select(l => new SetSlaEscalationsCommand.Rung(\n"
                    "                l.EscalationType.Trim(),\n"
                    "                l.Level,\n"
                    "                l.ThresholdPercent,\n"
                    "                l.RecipientType.Trim(),\n"
                    "                string.IsNullOrWhiteSpace(l.RecipientAddress) ? null : l.RecipientAddress.Trim(),\n"
                    "                string.IsNullOrWhiteSpace(l.Channel) ? EscalationChannel.Email : l.Channel.Trim()))]"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                SetSlaEscalationsRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SearchEscalationLog", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "Which escalations actually fired. Catalogue: the SLA panel on Request Detail.",
        "capability": SLA,
        "verb": "Get", "route": "/escalation-log",
        "command": [("int?", "ServiceRequestId"), ("string?", "Outcome"), ("int", "Take")],
        "request": [("int?", "ServiceRequestId"), ("string?", "Outcome"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchEscalationLogResponse.Row>", "Rows")],
        "responseSummary": "What was sent, to whom, and whether it arrived.",
        "responseDocs": {"Rows": "The log, most recent first."},
        "rules": [
            "RuleFor(x => x.Outcome).MaximumLength(20);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 500).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.ServiceRequestId",
                    "string.IsNullOrWhiteSpace(request.Outcome) ? null : request.Outcome.Trim()",
                    "request.Take ?? 100"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchEscalationLogRequest request,\n",
    },
]

if __name__ == "__main__":
    main(SPECS)
