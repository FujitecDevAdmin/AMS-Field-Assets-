"""
Slice specifications for the Identity module's Roles & Capabilities screen.

Catalogue features: Grant or deny one capability, and Field Asset Admin access
- which needs no code of its own, being a role that holds the field-asset
capabilities.

    python build/identity_slices_roles.py
"""
from slices import main

NS = "AMS.Modules.Identity"
PROJECT = "AMS.Modules.Identity"
MANAGE = "Capabilities.Identity.UserManage"
VIEW = "Capabilities.Identity.UserView"
ROLES = "Capabilities.Identity.RoleManage"

SPECS = [
    {
        "name": "SearchRoles", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The role list with its capability counts.",
        "capability": VIEW,
        "verb": "Get", "route": "/roles",
        "command": [("bool?", "IsActive")],
        "request": [("bool?", "IsActive")],
        "response": [("IReadOnlyList<SearchRolesResponse.Row>", "Rows")],
        "responseSummary": "Every role matching the filter. Roles are few; this one is not paged.",
        "responseDocs": {"Rows": "The roles."},
        "rules": [],
        "mapArgs": ["request.IsActive"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchRolesRequest request,\n",
    },
    {
        "name": "GetCapabilities", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "Every capability the application knows about, for the matrix.",
        "capability": VIEW,
        "verb": "Get", "route": "/capabilities",
        "command": [("string?", "Module")],
        "request": [("string?", "Module")],
        "response": [("IReadOnlyList<GetCapabilitiesResponse.Row>", "Rows")],
        "responseSummary": "The capability catalogue, grouped by owning module.",
        "responseDocs": {"Rows": "The capabilities."},
        "rules": ["RuleFor(x => x.Module).MaximumLength(60);"],
        "mapArgs": ["request.Module?.Trim()"],
        "mapCall": "request",
        "bind": "                [AsParameters] GetCapabilitiesRequest request,\n",
    },
    {
        "name": "CreateRole", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add a role. Catalogue screen: Roles & Capabilities.",
        "capability": ROLES,
        "verb": "Post", "route": "/roles",
        "command": [("string", "RoleName"), ("string?", "Description")],
        "request": [("string", "RoleName"), ("string?", "Description")],
        "response": [("int", "Id"), ("string", "RoleName")],
        "responseSummary": "The new role.",
        "responseDocs": {"Id": "The new role.", "RoleName": "As stored, trimmed."},
        "rules": [
            "RuleFor(x => x.RoleName).NotEmpty().MaximumLength(80);",
            "RuleFor(x => x.Description).MaximumLength(300);",
        ],
        "mapArgs": ["request.RoleName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()"],
        "mapCall": "request",
        "bind": "                CreateRoleRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/identity/roles/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateRole", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Rename a role or deactivate it.",
        "capability": ROLES,
        "verb": "Put", "route": "/roles/{roleId:int}",
        "command": [("int", "RoleId"), ("string", "RoleName"), ("string?", "Description"), ("bool", "IsActive")],
        "request": [("string", "RoleName"), ("string?", "Description"), ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "RoleName"), ("bool", "IsActive")],
        "responseSummary": "The updated role.",
        "responseDocs": {
            "Id": "The role edited.",
            "RoleName": "As stored, trimmed.",
            "IsActive": "An inactive role grants nothing, which is how a role is retired "
                        "without unpicking who holds it.",
        },
        "rules": [
            "RuleFor(x => x.RoleName).NotEmpty().MaximumLength(80);",
            "RuleFor(x => x.Description).MaximumLength(300);",
        ],
        "mapArgs": ["roleId", "request.RoleName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()",
                    "request.IsActive"],
        "mapCall": "request, roleId",
        "mapExtra": [("int", "roleId")],
        "bind": "                int roleId,\n                UpdateRoleRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SetRoleCapabilities", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Replace the capabilities a role grants. Catalogue: the capability matrix.",
        "capability": ROLES,
        "verb": "Put", "route": "/roles/{roleId:int}/capabilities",
        "command": [("int", "RoleId"), ("IReadOnlyList<string>", "CapabilityNames")],
        "request": [("IReadOnlyList<string>", "CapabilityNames")],
        "response": [("int", "RoleId"), ("IReadOnlyList<string>", "CapabilityNames")],
        "responseSummary": "What the role grants now.",
        "responseDocs": {
            "RoleId": "The role changed.",
            "CapabilityNames": "The complete set afterwards, not a delta.",
        },
        "rules": [
            "RuleFor(x => x.CapabilityNames).NotNull();",
            "RuleForEach(x => x.CapabilityNames).NotEmpty().MaximumLength(80);",
        ],
        "mapArgs": ["roleId", "request.CapabilityNames"],
        "mapCall": "request, roleId",
        "mapExtra": [("int", "roleId")],
        "bind": "                int roleId,\n                SetRoleCapabilitiesRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status400BadRequest"],
    },
    {
        "name": "SetUserCapabilityOverride", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Grant or deny one capability to one person. Catalogue: Grant or deny "
                   "one capability - a deny wins.",
        "capability": MANAGE,
        "verb": "Put", "route": "/users/{userId:int}/capability-overrides/{capabilityName}",
        "command": [("int", "UserId"), ("string", "CapabilityName"), ("bool", "IsGranted"),
                    ("string?", "Reason")],
        "request": [("bool", "IsGranted"), ("string?", "Reason")],
        "response": [("int", "UserId"), ("string", "CapabilityName"), ("bool", "IsGranted")],
        "responseSummary": "The override now in force.",
        "responseDocs": {
            "UserId": "The user.",
            "CapabilityName": "The capability.",
            "IsGranted": "False is a DENY, and a deny beats every role grant. That is the "
                         "point: one permission can be withdrawn without unpicking roles.",
        },
        "rules": [
            "RuleFor(x => x.Reason).MaximumLength(300);",
        ],
        "mapArgs": ["userId", "capabilityName", "request.IsGranted", "request.Reason?.Trim()"],
        "mapCall": "request, userId, capabilityName",
        "mapExtra": [("int", "userId"), ("string", "capabilityName")],
        "bind": "                int userId,\n                string capabilityName,\n"
                "                SetUserCapabilityOverrideRequest request,\n",
        "otherStatuses": ["Status404NotFound"],
    },
]

if __name__ == "__main__":
    main(SPECS)
