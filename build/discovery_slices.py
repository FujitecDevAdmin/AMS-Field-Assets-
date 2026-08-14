"""
Slice specifications for the Discovery module.

Catalogue screens: Agent Keys, the Discovered Devices queue, Asset Health, and
Installed Software with its catalogue. Six tables fed by an agent running on
every machine.

The shape that matters: the agent is not a user. It has no session, no branches
and nobody to grant anything to, so its endpoint authenticates with an API key
and everything else in the module is capability-gated as usual.

    python build/discovery_slices.py
"""
from slices import main

NS = "AMS.Modules.Discovery"
PROJECT = "AMS.Modules.Discovery"

VIEW = "Capabilities.Discovery.View"
MANAGE = "Capabilities.Discovery.Manage"
KEYS = "Capabilities.Discovery.AgentKeyManage"
CATALOG = "Capabilities.Discovery.SoftwareCatalogManage"

SPECS = [
    # ------------------------------------------------------------ the keys
    {
        "name": "SearchAgentKeys", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The agent keys and when each was last used. Catalogue: Agent Keys.",
        "capability": KEYS,
        "verb": "Get", "route": "/agent-keys",
        "command": [("bool", "ActiveOnly")],
        "request": [("bool?", "ActiveOnly")],
        "response": [("IReadOnlyList<SearchAgentKeysResponse.Row>", "Rows")],
        "responseSummary": "The keys. Never the secrets.",
        "responseDocs": {"Rows": "One row per key, most recently used first."},
        "rules": [],
        "mapArgs": ["request.ActiveOnly ?? false"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchAgentKeysRequest request,\n",
    },
    {
        "name": "IssueAgentKey", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Mint a key for an agent to use. Catalogue: Agent Keys.",
        "capability": KEYS,
        "verb": "Post", "route": "/agent-keys",
        "command": [("string", "KeyName")],
        "request": [("string", "KeyName")],
        "response": [("int", "Id"), ("string", "KeyName"), ("string", "Key"),
                     ("string", "KeyPrefix")],
        "responseSummary": "The key, shown once.",
        "responseDocs": {
            "Id": "The key row.",
            "KeyName": "What it is called — usually a site or a rollout.",
            "Key": "The secret. This is the ONLY time it is readable; the database keeps a hash.",
            "KeyPrefix": "The first twelve characters, which is what the screen shows afterwards.",
        },
        "rules": ["RuleFor(x => x.KeyName).NotEmpty().MaximumLength(100);"],
        "mapArgs": ["request.KeyName.Trim()"],
        "mapCall": "request",
        "bind": "                IssueAgentKeyRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "RevokeAgentKey", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Stop a key working. Catalogue: Agent Keys.",
        "capability": KEYS,
        "verb": "Post", "route": "/agent-keys/{id:int}/revocation",
        "command": [("int", "Id")],
        "request": [],
        "response": [("int", "Id"), ("string", "KeyName"), ("DateTime", "RevokedOnUtc")],
        "responseSummary": "The key, dead.",
        "responseDocs": {
            "Id": "The key row.",
            "KeyName": "What it was called.",
            "RevokedOnUtc": "When it stopped working.",
        },
        "rules": [],
        "mapArgs": ["id"],
        "mapCall": "new RevokeAgentKeyRequest(), id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # ------------------------------------------------------- the agent post
    {
        "name": "ReportInventory", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "What an agent found on one machine. Posted by the agent, not by a person.",
        # No capability: an agent is not a user. It presents an API key, and
        # the handler decides. See Capabilities.cs.
        "anonymous": True,
        "verb": "Post", "route": "/inventory",
        "command": [("string?", "ApiKey"), ("string", "Hostname"), ("string?", "SerialNumber"),
                    ("string?", "Manufacturer"), ("string?", "Model"),
                    ("string?", "OperatingSystem"), ("string?", "MacAddress"),
                    ("int?", "AssetId"),
                    ("ReportInventoryCommand.HealthReading?", "Health"),
                    ("IReadOnlyList<ReportInventoryCommand.SoftwareEntry>", "Software"),
                    ("string?", "RawPayloadJson")],
        "request": [("string", "Hostname"), ("string?", "SerialNumber"),
                    ("string?", "Manufacturer"), ("string?", "Model"),
                    ("string?", "OperatingSystem"), ("string?", "MacAddress"),
                    ("int?", "AssetId"),
                    ("ReportInventoryRequest.HealthReading?", "Health"),
                    ("IReadOnlyList<ReportInventoryRequest.SoftwareEntry>?", "Software"),
                    ("string?", "RawPayloadJson")],
        "response": [("int", "DiscoveredDeviceId"), ("string", "Status"),
                     ("bool", "IsNewDevice"), ("int?", "LinkedAssetId"),
                     ("int", "SoftwareRecorded"), ("int", "SoftwareRemoved")],
        "responseSummary": "What the report did.",
        "responseDocs": {
            "DiscoveredDeviceId": "The device row, new or updated.",
            "Status": "New, Linked, Registered or Ignored.",
            "IsNewDevice": "True the first time a machine reports.",
            "LinkedAssetId": "The asset it belongs to, once somebody has said so.",
            "SoftwareRecorded": "How many installations were seen this time.",
            "SoftwareRemoved": "How many previously seen installations have gone.",
        },
        "rules": [
            "RuleFor(x => x.Hostname).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.SerialNumber).MaximumLength(100);",
            "RuleFor(x => x.Manufacturer).MaximumLength(150);",
            "RuleFor(x => x.Model).MaximumLength(150);",
            "RuleFor(x => x.OperatingSystem).MaximumLength(150);",
            "RuleFor(x => x.MacAddress).MaximumLength(50);",
        ],
        "mapArgs": ["apiKey", "request.Hostname.Trim()",
                    "string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim()",
                    "string.IsNullOrWhiteSpace(request.Manufacturer) ? null : request.Manufacturer.Trim()",
                    "string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim()",
                    "string.IsNullOrWhiteSpace(request.OperatingSystem) ? null : request.OperatingSystem.Trim()",
                    "string.IsNullOrWhiteSpace(request.MacAddress) ? null : request.MacAddress.Trim()",
                    "request.AssetId",
                    "request.Health is null ? null : new ReportInventoryCommand.HealthReading(\n"
                    "                request.Health.CpuPercent,\n"
                    "                request.Health.MemoryPercent,\n"
                    "                request.Health.SystemDrivePercent,\n"
                    "                request.Health.BatteryHealthPercent,\n"
                    "                request.Health.UptimeHours,\n"
                    "                string.IsNullOrWhiteSpace(request.Health.LoggedInUser) ? null : request.Health.LoggedInUser.Trim())",
                    "[.. (request.Software ?? []).Select(s => new ReportInventoryCommand.SoftwareEntry(\n"
                    "                s.SoftwareName.Trim(),\n"
                    "                string.IsNullOrWhiteSpace(s.Version) ? null : s.Version.Trim(),\n"
                    "                string.IsNullOrWhiteSpace(s.Publisher) ? null : s.Publisher.Trim()))]",
                    "request.RawPayloadJson"],
        "mapCall": "request, apiKey",
        "mapExtra": [("string?", "apiKey")],
        "bind": "                [FromHeader(Name = \"X-Ams-Agent-Key\")] string? apiKey,\n"
                "                ReportInventoryRequest request,\n",
        "successStatus": "Status200OK",
        "otherStatuses": ["Status403Forbidden"],
    },

    # ----------------------------------------------------------- the queue
    {
        "name": "SearchDiscoveredDevices", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "Machines the agent has found. Catalogue: Discovered Devices.",
        "capability": VIEW,
        "verb": "Get", "route": "/devices",
        "command": [("string?", "Status"), ("string?", "Search"), ("bool", "UnresolvedOnly"),
                    ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Status"), ("string?", "Search"), ("bool?", "UnresolvedOnly"),
                    ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchDiscoveredDevicesResponse.Row>", "Rows"),
                     ("int", "TotalCount"), ("int", "UnresolvedCount")],
        "responseSummary": "One page of machines, most recently seen first.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Devices matching the filter.",
            "UnresolvedCount": "How many nobody has decided about. The queue length.",
        },
        "rules": [
            "RuleFor(x => x.Status).MaximumLength(20);",
            "RuleFor(x => x.Search).MaximumLength(200);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim()",
                    "string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()",
                    "request.UnresolvedOnly ?? false", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchDiscoveredDevicesRequest request,\n",
    },
    {
        "name": "ResolveDiscoveredDevice", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Say what a discovered machine is. Catalogue: Discovered Devices.",
        "capability": MANAGE,
        "verb": "Post", "route": "/devices/{id:int}/resolution",
        "command": [("int", "Id"), ("string", "Status"), ("int?", "LinkedAssetId")],
        "request": [("string", "Status"), ("int?", "LinkedAssetId")],
        "response": [("int", "Id"), ("string", "Status"), ("int?", "LinkedAssetId")],
        "responseSummary": "What was decided.",
        "responseDocs": {
            "Id": "The device.",
            "Status": "Linked, Registered or Ignored.",
            "LinkedAssetId": "The asset it belongs to, when it belongs to one.",
        },
        "rules": ["RuleFor(x => x.Status).NotEmpty().MaximumLength(20);"],
        "mapArgs": ["id", "request.Status.Trim()", "request.LinkedAssetId"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                ResolveDiscoveredDeviceRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # -------------------------------------------------- health and software
    {
        "name": "SearchAssetHealth", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "How the machines are doing. Catalogue: Asset Health.",
        "capability": VIEW,
        "verb": "Get", "route": "/health",
        "command": [("int?", "AssetId"), ("decimal?", "MinDrivePercent"),
                    ("int?", "NotSeenForHours"), ("int", "Skip"), ("int", "Take")],
        "request": [("int?", "AssetId"), ("decimal?", "MinDrivePercent"),
                    ("int?", "NotSeenForHours"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchAssetHealthResponse.Row>", "Rows"),
                     ("int", "TotalCount")],
        "responseSummary": "One page, worst first.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Machines matching the filter.",
        },
        "rules": [
            "RuleFor(x => x.MinDrivePercent).InclusiveBetween(0, 100)"
            ".When(x => x.MinDrivePercent.HasValue);",
            "RuleFor(x => x.NotSeenForHours).GreaterThan(0).When(x => x.NotSeenForHours.HasValue);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.AssetId", "request.MinDrivePercent", "request.NotSeenForHours",
                    "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchAssetHealthRequest request,\n",
    },
    {
        "name": "SearchInstalledSoftware", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What is installed, and whether we are licensed for it. "
                   "Catalogue: Installed Software.",
        "capability": VIEW,
        "verb": "Get", "route": "/software",
        "command": [("string?", "Search"), ("int?", "AssetId"), ("bool", "BlacklistedOnly"),
                    ("bool", "OverLicensedOnly"), ("bool", "IncludeRemoved")],
        "request": [("string?", "Search"), ("int?", "AssetId"), ("bool?", "BlacklistedOnly"),
                    ("bool?", "OverLicensedOnly"), ("bool?", "IncludeRemoved")],
        "response": [("IReadOnlyList<SearchInstalledSoftwareResponse.Row>", "Rows"),
                     ("int", "BlacklistedInstallCount"), ("int", "OverLicensedTitleCount")],
        "responseSummary": "One row per title, most installed first.",
        "responseDocs": {
            "Rows": "The titles, with how many machines have each.",
            "BlacklistedInstallCount": "Installations of software nobody is meant to have.",
            "OverLicensedTitleCount": "Titles installed on more machines than there are seats.",
        },
        "rules": ["RuleFor(x => x.Search).MaximumLength(300);"],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()",
                    "request.AssetId", "request.BlacklistedOnly ?? false",
                    "request.OverLicensedOnly ?? false", "request.IncludeRemoved ?? false"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchInstalledSoftwareRequest request,\n",
    },
    {
        "name": "SetSoftwareCatalogEntry", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Record what we are licensed for, or blacklist a title. "
                   "Catalogue: Software Catalogue.",
        "capability": CATALOG,
        "verb": "Put", "route": "/software-catalog",
        "command": [("string", "SoftwareName"), ("string?", "Publisher"),
                    ("int?", "LicensedSeats"), ("int?", "ContractId"),
                    ("bool", "IsBlacklisted"), ("bool", "IsActive")],
        "request": [("string", "SoftwareName"), ("string?", "Publisher"),
                    ("int?", "LicensedSeats"), ("int?", "ContractId"),
                    ("bool?", "IsBlacklisted"), ("bool?", "IsActive")],
        "response": [("int", "Id"), ("string", "SoftwareName"), ("int?", "LicensedSeats"),
                     ("int", "InstalledCount"), ("bool", "IsOverLicensed")],
        "responseSummary": "The entry, and how it stands against what is installed.",
        "responseDocs": {
            "Id": "The catalogue entry.",
            "SoftwareName": "The title, as the agent reports it.",
            "LicensedSeats": "How many we bought.",
            "InstalledCount": "How many machines have it.",
            "IsOverLicensed": "Whether the second number is larger than the first.",
        },
        "rules": [
            "RuleFor(x => x.SoftwareName).NotEmpty().MaximumLength(300);",
            "RuleFor(x => x.Publisher).MaximumLength(200);",
            "RuleFor(x => x.LicensedSeats).GreaterThanOrEqualTo(0)"
            ".When(x => x.LicensedSeats.HasValue);",
        ],
        "mapArgs": ["request.SoftwareName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Publisher) ? null : request.Publisher.Trim()",
                    "request.LicensedSeats", "request.ContractId",
                    "request.IsBlacklisted ?? false", "request.IsActive ?? true"],
        "mapCall": "request",
        "bind": "                SetSoftwareCatalogEntryRequest request,\n",
        "otherStatuses": ["Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
