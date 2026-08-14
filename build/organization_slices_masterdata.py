"""
Slice specifications for the Organization module's master-data screens.

Catalogue features 1, 2, 3, 9, 10, 11: Branches and locations, Departments,
Vendors, Regions, Put a branch in a region, Branch time zone.

Region, Department and Vendor are the same shape - a named lookup with an
active flag and a unique name - so their specs are BUILT rather than typed.
Location is not: it carries a code, a region, a time zone and the
one-head-office rule, so it is written out.

    python build/organization_slices_masterdata.py
"""
from slices import main

NS = "AMS.Modules.Organization"
PROJECT = "AMS.Modules.Organization"
MANAGE = "Capabilities.Organization.Manage"
VIEW = "Capabilities.Organization.View"


def lookup_specs(entity, plural, name_field, name_length, route, extra_fields=None,
                 extra_rules=None, conflict_code=None, conflict_message=None):
    """
    Search / Create / Update for a named lookup table.

    Everything these have in common is genuinely common: a unique name enforced
    by an index, an IsActive flag that retires a row without deleting it, and no
    paging because these tables hold tens of rows, not thousands.
    """
    extra_fields = extra_fields or []
    extra_rules = extra_rules or []
    extra_command = [(t, n) for t, n, *_ in extra_fields]

    return [
        {
            "name": f"Search{plural}", "kind": "query", "ns": NS, "project": PROJECT,
            "summary": f"The {entity.lower()} list. Catalogue screen: {plural}.",
            "capability": VIEW,
            "verb": "Get", "route": route,
            "command": [("bool?", "IsActive"), ("string?", "Search")],
            "request": [("bool?", "IsActive"), ("string?", "Search")],
            "response": [(f"IReadOnlyList<Search{plural}Response.Row>", "Rows")],
            "responseSummary": f"Every {entity.lower()} matching the filter. These tables hold "
                               "tens of rows, so the list is not paged.",
            "responseDocs": {"Rows": f"The {plural.lower()}."},
            "rules": ["RuleFor(x => x.Search).MaximumLength(100);"],
            "mapArgs": ["request.IsActive", "request.Search?.Trim()"],
            "mapCall": "request",
            "bind": f"                [AsParameters] Search{plural}Request request,\n",
        },
        {
            "name": f"Create{entity}", "kind": "command", "ns": NS, "project": PROJECT,
            "summary": f"Add a {entity.lower()}. Catalogue screen: {plural}.",
            "capability": MANAGE,
            "verb": "Post", "route": route,
            "command": [("string", name_field)] + extra_command,
            "request": [("string", name_field)] + extra_command,
            "response": [("int", "Id"), ("string", name_field)],
            "responseSummary": f"The new {entity.lower()}.",
            "responseDocs": {
                "Id": f"The new {entity.lower()}.",
                name_field: "As stored, trimmed.",
            },
            "rules": [f"RuleFor(x => x.{name_field}).NotEmpty().MaximumLength({name_length});"] + extra_rules,
            "mapArgs": [f"request.{name_field}.Trim()"] + [f"request.{n}" for _, n, *_ in extra_fields],
            "mapCall": "request",
            "bind": f"                Create{entity}Request request,\n",
            "successStatus": "Status201Created",
            "result": f"ToCreatedResult(response => $\"{route}/{{response.Id}}\")",
            "otherStatuses": ["Status409Conflict"],
        },
        {
            "name": f"Update{entity}", "kind": "command", "ns": NS, "project": PROJECT,
            "summary": f"Rename a {entity.lower()} or retire it. Catalogue screen: {plural}.",
            "capability": MANAGE,
            "verb": "Put", "route": route + "/{id:int}",
            "command": [("int", "Id"), ("string", name_field)] + extra_command + [("bool", "IsActive")],
            "request": [("string", name_field)] + extra_command + [("bool", "IsActive")],
            "response": [("int", "Id"), ("string", name_field), ("bool", "IsActive")],
            "responseSummary": f"The updated {entity.lower()}.",
            "responseDocs": {
                "Id": f"The {entity.lower()} edited.",
                name_field: "As stored, trimmed.",
                "IsActive": "Retiring is deactivation, never deletion: rows elsewhere still "
                            "point at this one.",
            },
            "rules": [f"RuleFor(x => x.{name_field}).NotEmpty().MaximumLength({name_length});"] + extra_rules,
            "mapArgs": ["id", f"request.{name_field}.Trim()"]
                       + [f"request.{n}" for _, n, *_ in extra_fields]
                       + ["request.IsActive"],
            "mapCall": "request, id",
            "mapExtra": [("int", "id")],
            "bind": f"                int id,\n                Update{entity}Request request,\n",
            "otherStatuses": ["Status404NotFound", "Status409Conflict"],
        },
    ]


SPECS = []

SPECS += lookup_specs(
    "Region", "Regions", "RegionName", 60, "/regions",
    extra_fields=[("string?", "Description")],
    extra_rules=["RuleFor(x => x.Description).MaximumLength(300);"])

SPECS += lookup_specs(
    "Department", "Departments", "DepartmentName", 100, "/departments")

SPECS += lookup_specs(
    "Vendor", "Vendors", "VendorName", 150, "/vendors",
    extra_fields=[("string?", "ContactPerson"), ("string?", "Phone"), ("string?", "Email")],
    extra_rules=[
        "RuleFor(x => x.ContactPerson).MaximumLength(120);",
        "RuleFor(x => x.Phone).MaximumLength(40);",
        "RuleFor(x => x.Email).MaximumLength(256).EmailAddress()",
        "    .When(x => !string.IsNullOrWhiteSpace(x.Email));",
    ])

# Location is its own shape: a code, a region, a time zone, and one head office.
SPECS += [
    {
        "name": "SearchLocations", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The branch list. Catalogue screen: Branches.",
        "capability": VIEW,
        "verb": "Get", "route": "/locations",
        "command": [("bool?", "IsActive"), ("int?", "RegionId"), ("string?", "Search")],
        "request": [("bool?", "IsActive"), ("int?", "RegionId"), ("string?", "Search")],
        "response": [("IReadOnlyList<SearchLocationsResponse.Row>", "Rows")],
        "responseSummary": "Every branch matching the filter.",
        "responseDocs": {"Rows": "The branches."},
        "rules": ["RuleFor(x => x.Search).MaximumLength(100);"],
        "mapArgs": ["request.IsActive", "request.RegionId", "request.Search?.Trim()"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchLocationsRequest request,\n",
    },
    {
        "name": "CreateLocation", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Open a branch. Catalogue: Branches and locations, Put a branch in a "
                   "region, Branch time zone.",
        "capability": MANAGE,
        "verb": "Post", "route": "/locations",
        "command": [("string", "LocationCode"), ("string", "LocationName"), ("int?", "RegionId"),
                    ("string", "TimeZoneId"), ("bool", "IsHeadOffice")],
        "request": [("string", "LocationCode"), ("string", "LocationName"), ("int?", "RegionId"),
                    ("string", "TimeZoneId"), ("bool", "IsHeadOffice")],
        "response": [("int", "Id"), ("string", "LocationCode"), ("string", "LocationName"),
                     ("bool", "IsHeadOffice")],
        "responseSummary": "The new branch.",
        "responseDocs": {
            "Id": "The new branch.",
            "LocationCode": "Unique, upper-cased.",
            "LocationName": "As stored, trimmed.",
            "IsHeadOffice": "At most one branch in the whole system has this.",
        },
        "rules": [
            "RuleFor(x => x.LocationCode).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.LocationName).NotEmpty().MaximumLength(100);",
            "// Not optional: a branch without a time zone cannot say what 09:00 means",
            "// there, and every SLA measurement taken against it would be wrong.",
            "RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);",
            "RuleFor(x => x.RegionId).GreaterThan(0).When(x => x.RegionId.HasValue);",
        ],
        "mapArgs": ["request.LocationCode.Trim().ToUpperInvariant()", "request.LocationName.Trim()",
                    "request.RegionId", "request.TimeZoneId.Trim()", "request.IsHeadOffice"],
        "mapCall": "request",
        "bind": "                CreateLocationRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/organization/locations/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateLocation", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit a branch, move it between regions, or retire it.",
        "capability": MANAGE,
        "verb": "Put", "route": "/locations/{id:int}",
        "command": [("int", "Id"), ("string", "LocationCode"), ("string", "LocationName"),
                    ("int?", "RegionId"), ("string", "TimeZoneId"), ("bool", "IsHeadOffice"),
                    ("bool", "IsActive")],
        "request": [("string", "LocationCode"), ("string", "LocationName"), ("int?", "RegionId"),
                    ("string", "TimeZoneId"), ("bool", "IsHeadOffice"), ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "LocationCode"), ("bool", "IsHeadOffice"),
                     ("bool", "IsActive")],
        "responseSummary": "The updated branch.",
        "responseDocs": {
            "Id": "The branch edited.",
            "LocationCode": "Unique, upper-cased.",
            "IsHeadOffice": "At most one across the system.",
            "IsActive": "Retiring is deactivation; assets and employees still point here.",
        },
        "rules": [
            "RuleFor(x => x.LocationCode).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.LocationName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(64);",
            "RuleFor(x => x.RegionId).GreaterThan(0).When(x => x.RegionId.HasValue);",
        ],
        "mapArgs": ["id", "request.LocationCode.Trim().ToUpperInvariant()", "request.LocationName.Trim()",
                    "request.RegionId", "request.TimeZoneId.Trim()", "request.IsHeadOffice",
                    "request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateLocationRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
