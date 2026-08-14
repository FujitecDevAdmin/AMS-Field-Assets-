"""
Slice specifications for the Notifications module.

Catalogue screens: the notification bell, E-mail Settings, and the outbox
queue an administrator looks at when somebody says they never got the message.

The module is three tables and does one thing: it is the only way anything in
this system tells somebody something. Sending inline from a request thread
loses the message when SMTP is down, and nobody finds out — so everything is
queued, and a dispatcher drains the queue.

    python build/notifications_slices.py
"""
from slices import main

NS = "AMS.Modules.Notifications"
PROJECT = "AMS.Modules.Notifications"

EMAIL = "Capabilities.Notifications.EmailSettingManage"
OUTBOX = "Capabilities.Notifications.OutboxManage"

SPECS = [
    # ---------------------------------------------------------- the bell
    {
        "name": "SearchMyNotifications", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What I have not read. Catalogue: the notification bell.",
        # No capability: every signed-in user reads their own, and a capability
        # would be a lie, because withdrawing it would stop somebody being told
        # things about their own work.
        "verb": "Get", "route": "/notifications/mine",
        "command": [("int", "UserId"), ("bool", "UnreadOnly"), ("int", "Take")],
        "request": [("bool?", "UnreadOnly"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchMyNotificationsResponse.Row>", "Rows"),
                     ("int", "UnreadCount")],
        "responseSummary": "My notifications, newest first.",
        "responseDocs": {
            "Rows": "The page.",
            "UnreadCount": "The number on the bell. Counted over everything, not the page.",
        },
        "rules": ["RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);"],
        "mapArgs": ["userId", "request.UnreadOnly ?? false", "request.Take ?? 50"],
        "mapCall": "request, currentUser.Id",
        "mapExtra": [("int", "userId")],
        "bind": "                [AsParameters] SearchMyNotificationsRequest request,\n"
                "                ICurrentUser currentUser,\n",
    },
    {
        "name": "MarkNotificationsRead", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Clear the bell, or one line of it. Catalogue: the notification bell.",
        "verb": "Post", "route": "/notifications/mine/read",
        "command": [("int", "UserId"), ("IReadOnlyList<long>", "Ids"), ("bool", "All")],
        "request": [("IReadOnlyList<long>?", "Ids"), ("bool?", "All")],
        "response": [("int", "MarkedCount"), ("int", "UnreadCount")],
        "responseSummary": "How many were cleared, and what is left.",
        "responseDocs": {
            "MarkedCount": "How many changed. Already-read lines are not counted twice.",
            "UnreadCount": "The number the bell should now show.",
        },
        "rules": [],
        "mapArgs": ["userId", "request.Ids ?? []", "request.All ?? false"],
        "mapCall": "request, currentUser.Id",
        "mapExtra": [("int", "userId")],
        "bind": "                MarkNotificationsReadRequest request,\n"
                "                ICurrentUser currentUser,\n",
    },

    # -------------------------------------------------------- SMTP profiles
    {
        "name": "SearchEmailSettings", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The SMTP profiles. Catalogue: E-mail Settings.",
        "capability": EMAIL,
        "verb": "Get", "route": "/email-settings",
        "command": [("bool", "ActiveOnly")],
        "request": [("bool?", "ActiveOnly")],
        "response": [("IReadOnlyList<SearchEmailSettingsResponse.Row>", "Rows")],
        "responseSummary": "The profiles. Never the passwords.",
        "responseDocs": {"Rows": "One row per profile, default first."},
        "rules": [],
        "mapArgs": ["request.ActiveOnly ?? false"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchEmailSettingsRequest request,\n",
    },
    {
        "name": "CreateEmailSetting", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add an SMTP profile. Catalogue: E-mail Settings.",
        "capability": EMAIL,
        "verb": "Post", "route": "/email-settings",
        "command": [("string", "ProfileName"), ("string", "Host"), ("int", "Port"),
                    ("bool", "UseSsl"), ("string", "FromAddress"), ("string?", "Username"),
                    ("string?", "Password"), ("bool", "IsDefault")],
        "request": [("string", "ProfileName"), ("string", "Host"), ("int?", "Port"),
                    ("bool?", "UseSsl"), ("string", "FromAddress"), ("string?", "Username"),
                    ("string?", "Password"), ("bool?", "IsDefault")],
        "response": [("int", "Id"), ("string", "ProfileName"), ("bool", "IsDefault")],
        "responseSummary": "The profile, live.",
        "responseDocs": {
            "Id": "The profile.",
            "ProfileName": "What it is called.",
            "IsDefault": "Whether the dispatcher sends through it. At most one profile may.",
        },
        "rules": [
            "RuleFor(x => x.ProfileName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.Host).NotEmpty().MaximumLength(200);",
            "RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port.HasValue);",
            "RuleFor(x => x.FromAddress).NotEmpty().EmailAddress().MaximumLength(256);",
            "RuleFor(x => x.Username).MaximumLength(200);",
        ],
        "mapArgs": ["request.ProfileName.Trim()", "request.Host.Trim()", "request.Port ?? 25",
                    "request.UseSsl ?? true", "request.FromAddress.Trim()",
                    "string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim()",
                    "string.IsNullOrEmpty(request.Password) ? null : request.Password",
                    "request.IsDefault ?? false"],
        "mapCall": "request",
        "bind": "                CreateEmailSettingRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateEmailSetting", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit an SMTP profile or retire it. Catalogue: E-mail Settings.",
        "capability": EMAIL,
        "verb": "Put", "route": "/email-settings/{id:int}",
        "command": [("int", "Id"), ("string", "ProfileName"), ("string", "Host"), ("int", "Port"),
                    ("bool", "UseSsl"), ("string", "FromAddress"), ("string?", "Username"),
                    ("string?", "Password"), ("bool", "IsDefault"), ("bool", "IsActive")],
        "request": [("string", "ProfileName"), ("string", "Host"), ("int?", "Port"),
                    ("bool?", "UseSsl"), ("string", "FromAddress"), ("string?", "Username"),
                    ("string?", "Password"), ("bool?", "IsDefault"), ("bool?", "IsActive")],
        "response": [("int", "Id"), ("string", "ProfileName"), ("bool", "IsActive")],
        "responseSummary": "The profile as it now stands.",
        "responseDocs": {
            "Id": "The profile.",
            "ProfileName": "What it is called.",
            "IsActive": "Whether the dispatcher may use it.",
        },
        "rules": [
            "RuleFor(x => x.ProfileName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.Host).NotEmpty().MaximumLength(200);",
            "RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port.HasValue);",
            "RuleFor(x => x.FromAddress).NotEmpty().EmailAddress().MaximumLength(256);",
            "RuleFor(x => x.Username).MaximumLength(200);",
        ],
        "mapArgs": ["id", "request.ProfileName.Trim()", "request.Host.Trim()", "request.Port ?? 25",
                    "request.UseSsl ?? true", "request.FromAddress.Trim()",
                    "string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim()",
                    "string.IsNullOrEmpty(request.Password) ? null : request.Password",
                    "request.IsDefault ?? false", "request.IsActive ?? true"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateEmailSettingRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # ------------------------------------------------------------ the queue
    {
        "name": "SearchEmailOutbox", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What is queued, sent and stuck. Catalogue: the outbox queue.",
        "capability": OUTBOX,
        "verb": "Get", "route": "/outbox",
        "command": [("string?", "Status"), ("string?", "SourceType"), ("long?", "SourceId"),
                    ("string?", "Search"), ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Status"), ("string?", "SourceType"), ("long?", "SourceId"),
                    ("string?", "Search"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchEmailOutboxResponse.Row>", "Rows"),
                     ("int", "TotalCount"), ("int", "PendingCount"), ("int", "FailedCount")],
        "responseSummary": "The queue, newest first.",
        "responseDocs": {
            "Rows": "The page. Bodies are not included; the list is a list.",
            "TotalCount": "Messages matching the filter.",
            "PendingCount": "How many are still waiting, over the whole queue.",
            "FailedCount": "How many have been given up on. The number that needs somebody.",
        },
        "rules": [
            "RuleFor(x => x.Status).MaximumLength(20);",
            "RuleFor(x => x.SourceType).MaximumLength(40);",
            "RuleFor(x => x.Search).MaximumLength(300);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim()",
                    "string.IsNullOrWhiteSpace(request.SourceType) ? null : request.SourceType.Trim()",
                    "request.SourceId",
                    "string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()",
                    "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchEmailOutboxRequest request,\n",
    },
    {
        "name": "RequeueEmail", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Try a failed message again. Catalogue: the outbox queue.",
        "capability": OUTBOX,
        "verb": "Post", "route": "/outbox/{id:long}/requeue",
        "command": [("long", "Id")],
        "request": [],
        "response": [("long", "Id"), ("string", "Status"), ("int", "AttemptCount")],
        "responseSummary": "The message, waiting again.",
        "responseDocs": {
            "Id": "The message.",
            "Status": "Always Pending.",
            "AttemptCount": "Reset to zero, so it gets a full set of tries at the corrected address.",
        },
        "rules": [],
        "mapArgs": ["id"],
        "mapCall": "new RequeueEmailRequest(), id",
        "mapExtra": [("long", "id")],
        "bind": "                long id,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
