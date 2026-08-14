"""
Slice specifications for the Assets module's master-data screens.

Catalogue screens: Asset Types & Custom Fields, Asset Statuses, and (new in
Revision 3) Asset Classes & Chart of Accounts.

Revision 3 renamed AssetCategory to AssetType and added the finance taxonomy,
so every route, field and slice name below moved with it. `AssetType` is what
an asset IS and drives behaviour; `AssetClass` is what the ACCOUNTS call it.
They are separate because 86 technical groups appear under more than one class.

    python build/assets_slices_taxonomy.py
"""
from slices import main

NS = "AMS.Modules.Assets"
PROJECT = "AMS.Modules.Assets"

# Taxonomy is Super Admin only; the register itself is a branch job. See R3-4
# in the design script for why the split is by audience and not for symmetry.
MANAGE = "Capabilities.Assets.TaxonomyManage"
VIEW = "Capabilities.Assets.View"

# The seven behaviour flags, as (command type, name, request type, rule).
FLAGS = [
    ("IsAllocatable", "Whether an asset of this type can be issued to a person."),
    ("IsPhysical", "0 for software and licences: no serial, no location, no verification."),
    ("IsBulkDefault", "Whether new assets of this type default to a bulk line with a quantity."),
    ("TracksHardware", "Whether the hardware detail record applies."),
    ("TracksSoftware", "Whether the software detail record applies."),
    ("TracksVehicle", "Whether the vehicle detail record applies."),
    ("TracksCalibration", "Whether the instrument calibration record applies."),
]
FLAG_COMMAND = [("bool", n) for n, _ in FLAGS]
FLAG_REQUEST = [("bool?", n) for n, _ in FLAGS]
FLAG_DOCS = {n: d for n, d in FLAGS}

SPECS = [
    # ----------------------------------------------------------- asset types
    {
        "name": "SearchAssetTypes", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The asset type tree. Catalogue screen: Asset Types and Custom Fields.",
        "capability": VIEW,
        "verb": "Get", "route": "/types",
        "command": [("bool?", "IsActive")],
        "request": [("bool?", "IsActive")],
        "response": [("IReadOnlyList<SearchAssetTypesResponse.Row>", "Rows")],
        "responseSummary": "Every type, flat, with its parent id. The client builds the tree.",
        "responseDocs": {"Rows": "The types, with their behaviour flags."},
        "rules": [],
        "mapArgs": ["request.IsActive"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchAssetTypesRequest request,\n",
    },
    {
        "name": "CreateAssetType", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add an asset type. Catalogue: Say what a type of asset can do.",
        "capability": MANAGE,
        "verb": "Post", "route": "/types",
        "command": [("string", "TypeName"), ("int?", "ParentAssetTypeId")] + FLAG_COMMAND,
        "request": [("string", "TypeName"), ("int?", "ParentAssetTypeId")] + FLAG_REQUEST,
        "response": [("int", "Id"), ("string", "TypeName")],
        "responseSummary": "The new type.",
        "responseDocs": {"Id": "The new type.", "TypeName": "Unique, trimmed."},
        "rules": [
            "RuleFor(x => x.TypeName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.ParentAssetTypeId).GreaterThan(0).When(x => x.ParentAssetTypeId.HasValue);",
        ],
        "mapArgs": ["request.TypeName.Trim()", "request.ParentAssetTypeId"]
                   + [f"request.{n} ?? {'true' if n in ('IsAllocatable', 'IsPhysical') else 'false'}"
                      for n, _ in FLAGS],
        "mapCall": "request",
        "bind": "                CreateAssetTypeRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/assets/types/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateAssetType", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Rename a type, move it in the tree, change what it can do, or retire it.",
        "capability": MANAGE,
        "verb": "Put", "route": "/types/{id:int}",
        "command": [("int", "Id"), ("string", "TypeName"), ("int?", "ParentAssetTypeId")]
                   + FLAG_COMMAND + [("bool", "IsActive")],
        "request": [("string", "TypeName"), ("int?", "ParentAssetTypeId")]
                   + FLAG_REQUEST + [("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "TypeName"), ("bool", "IsActive")],
        "responseSummary": "The updated type.",
        "responseDocs": {
            "Id": "The type edited.",
            "TypeName": "Unique, trimmed.",
            "IsActive": "Retiring is deactivation: assets and custom fields point here.",
        },
        "rules": [
            "RuleFor(x => x.TypeName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.ParentAssetTypeId).GreaterThan(0).When(x => x.ParentAssetTypeId.HasValue);",
        ],
        "mapArgs": ["id", "request.TypeName.Trim()", "request.ParentAssetTypeId"]
                   + [f"request.{n} ?? {'true' if n in ('IsAllocatable', 'IsPhysical') else 'false'}"
                      for n, _ in FLAGS]
                   + ["request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateAssetTypeRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # --------------------------------------------------------- asset classes
    {
        "name": "SearchAssetClasses", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The finance taxonomy. Catalogue screen: Asset Classes and Chart of Accounts.",
        "capability": VIEW,
        "verb": "Get", "route": "/classes",
        "command": [("bool?", "IsActive")],
        "request": [("bool?", "IsActive")],
        "response": [("IReadOnlyList<SearchAssetClassesResponse.Row>", "Rows")],
        "responseSummary": "Every class with the reporting category it rolls up to.",
        "responseDocs": {"Rows": "The classes, in code order."},
        "rules": [],
        "mapArgs": ["request.IsActive"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchAssetClassesRequest request,\n",
    },
    {
        "name": "CreateAssetClass", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add an asset class. Catalogue: Classify an asset for the accounts.",
        "capability": MANAGE,
        "verb": "Post", "route": "/classes",
        "command": [("string", "ClassCode"), ("string", "ClassName"), ("string", "ReportingCategory"),
                    ("bool", "IsDepreciable"), ("bool", "IsIntangible")],
        "request": [("string", "ClassCode"), ("string", "ClassName"), ("string", "ReportingCategory"),
                    ("bool?", "IsDepreciable"), ("bool?", "IsIntangible")],
        "response": [("int", "Id"), ("string", "ClassCode"), ("string", "ClassName")],
        "responseSummary": "The new class.",
        "responseDocs": {
            "Id": "The new class.",
            "ClassCode": "Unique. The importer matches on it, so it is the register's own spelling.",
            "ClassName": "Unique.",
        },
        "rules": [
            "RuleFor(x => x.ClassCode).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.ClassName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.ReportingCategory).NotEmpty().MaximumLength(100);",
        ],
        "mapArgs": ["request.ClassCode.Trim()", "request.ClassName.Trim()",
                    "request.ReportingCategory.Trim()",
                    "request.IsDepreciable ?? true", "request.IsIntangible ?? false"],
        "mapCall": "request",
        "bind": "                CreateAssetClassRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/assets/classes/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateAssetClass", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit an asset class or retire it.",
        "capability": MANAGE,
        "verb": "Put", "route": "/classes/{id:int}",
        "command": [("int", "Id"), ("string", "ClassCode"), ("string", "ClassName"),
                    ("string", "ReportingCategory"), ("bool", "IsDepreciable"),
                    ("bool", "IsIntangible"), ("bool", "IsActive")],
        "request": [("string", "ClassCode"), ("string", "ClassName"), ("string", "ReportingCategory"),
                    ("bool?", "IsDepreciable"), ("bool?", "IsIntangible"), ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "ClassCode"), ("bool", "IsActive")],
        "responseSummary": "The updated class.",
        "responseDocs": {
            "Id": "The class edited.",
            "ClassCode": "Unique.",
            "IsActive": "Retiring is deactivation: assets already classified keep pointing here.",
        },
        "rules": [
            "RuleFor(x => x.ClassCode).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.ClassName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.ReportingCategory).NotEmpty().MaximumLength(100);",
        ],
        "mapArgs": ["id", "request.ClassCode.Trim()", "request.ClassName.Trim()",
                    "request.ReportingCategory.Trim()",
                    "request.IsDepreciable ?? true", "request.IsIntangible ?? false",
                    "request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateAssetClassRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # ------------------------------------------------------ chart of accounts
    {
        "name": "SearchChartOfAccounts", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The ledger codes an asset's finance record points at.",
        "capability": VIEW,
        "verb": "Get", "route": "/chart-of-accounts",
        "command": [("bool?", "IsActive")],
        "request": [("bool?", "IsActive")],
        "response": [("IReadOnlyList<SearchChartOfAccountsResponse.Row>", "Rows")],
        "responseSummary": "Every code with its description.",
        "responseDocs": {"Rows": "The codes, in code order."},
        "rules": [],
        "mapArgs": ["request.IsActive"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchChartOfAccountsRequest request,\n",
    },
    {
        "name": "CreateChartOfAccount", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add a chart-of-account code.",
        "capability": MANAGE,
        "verb": "Post", "route": "/chart-of-accounts",
        "command": [("string", "CoaCode"), ("string?", "Description")],
        "request": [("string", "CoaCode"), ("string?", "Description")],
        "response": [("int", "Id"), ("string", "CoaCode")],
        "responseSummary": "The new code.",
        "responseDocs": {
            "Id": "The new code.",
            "CoaCode": "Unique. Stored once so 7,000 assets cannot hold 7,000 copies of one description.",
        },
        "rules": [
            "RuleFor(x => x.CoaCode).NotEmpty().MaximumLength(30);",
            "RuleFor(x => x.Description).MaximumLength(200);",
        ],
        "mapArgs": ["request.CoaCode.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()"],
        "mapCall": "request",
        "bind": "                CreateChartOfAccountRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/assets/chart-of-accounts/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateChartOfAccount", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit a chart-of-account code's description, or retire it.",
        "capability": MANAGE,
        "verb": "Put", "route": "/chart-of-accounts/{id:int}",
        "command": [("int", "Id"), ("string", "CoaCode"), ("string?", "Description"), ("bool", "IsActive")],
        "request": [("string", "CoaCode"), ("string?", "Description"), ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "CoaCode"), ("bool", "IsActive")],
        "responseSummary": "The updated code.",
        "responseDocs": {
            "Id": "The code edited.",
            "CoaCode": "Unique.",
            "IsActive": "Retiring hides it from pickers; finance records already pointing here keep it.",
        },
        "rules": [
            "RuleFor(x => x.CoaCode).NotEmpty().MaximumLength(30);",
            "RuleFor(x => x.Description).MaximumLength(200);",
        ],
        "mapArgs": ["id", "request.CoaCode.Trim()",
                    "string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()",
                    "request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateChartOfAccountRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # -------------------------------------------------------------- statuses
    {
        "name": "SearchAssetStatuses", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The asset status lookup. Catalogue screen: Asset Statuses.",
        "capability": VIEW,
        "verb": "Get", "route": "/statuses",
        "command": [("bool?", "IsActive")],
        "request": [("bool?", "IsActive")],
        "response": [("IReadOnlyList<SearchAssetStatusesResponse.Row>", "Rows")],
        "responseSummary": "Every status, in display order.",
        "responseDocs": {"Rows": "The statuses."},
        "rules": [],
        "mapArgs": ["request.IsActive"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchAssetStatusesRequest request,\n",
    },
    {
        "name": "CreateAssetStatus", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add an asset status. Catalogue: Status lookup maintenance.",
        "capability": MANAGE,
        "verb": "Post", "route": "/statuses",
        "command": [("string", "StatusName"), ("bool", "IsTerminal"), ("int", "DisplayOrder")],
        "request": [("string", "StatusName"), ("bool", "IsTerminal"), ("int?", "DisplayOrder")],
        "response": [("int", "Id"), ("string", "StatusName"), ("bool", "IsTerminal")],
        "responseSummary": "The new status.",
        "responseDocs": {
            "Id": "The new status.",
            "StatusName": "Unique, trimmed.",
            "IsTerminal": "A terminal status ends the asset's working life - Scrapped, Lost and "
                          "Disposed are the seeded ones. An asset in a terminal status cannot be "
                          "allocated again.",
        },
        "rules": [
            "RuleFor(x => x.StatusName).NotEmpty().MaximumLength(50);",
            "RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);",
        ],
        "mapArgs": ["request.StatusName.Trim()", "request.IsTerminal", "request.DisplayOrder ?? 0"],
        "mapCall": "request",
        "bind": "                CreateAssetStatusRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/assets/statuses/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateAssetStatus", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Rename a status, reorder it, or retire it.",
        "capability": MANAGE,
        "verb": "Put", "route": "/statuses/{id:int}",
        "command": [("int", "Id"), ("string", "StatusName"), ("bool", "IsTerminal"),
                    ("int", "DisplayOrder"), ("bool", "IsActive")],
        "request": [("string", "StatusName"), ("bool", "IsTerminal"), ("int?", "DisplayOrder"),
                    ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "StatusName"), ("bool", "IsActive")],
        "responseSummary": "The updated status.",
        "responseDocs": {
            "Id": "The status edited.",
            "StatusName": "Unique, trimmed.",
            "IsActive": "Retiring is deactivation: assets currently in this status keep it.",
        },
        "rules": [
            "RuleFor(x => x.StatusName).NotEmpty().MaximumLength(50);",
            "RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);",
        ],
        "mapArgs": ["id", "request.StatusName.Trim()", "request.IsTerminal",
                    "request.DisplayOrder ?? 0", "request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateAssetStatusRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },

    # --------------------------------------------------------- custom fields
    {
        "name": "GetAssetTypeCustomFields", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The custom fields defined for one asset type, with their dropdown "
                   "options. Catalogue: Define custom fields.",
        "capability": VIEW,
        "verb": "Get", "route": "/types/{assetTypeId:int}/custom-fields",
        "command": [("int", "AssetTypeId"), ("bool", "IncludeInactive")],
        "request": [("int", "AssetTypeId"), ("bool?", "IncludeInactive")],
        "response": [("int", "AssetTypeId"),
                     ("IReadOnlyList<GetAssetTypeCustomFieldsResponse.Row>", "Rows")],
        "responseSummary": "What the asset form must render for this type.",
        "responseDocs": {
            "AssetTypeId": "The type asked about.",
            "Rows": "Its fields, in display order.",
        },
        "rules": ["RuleFor(x => x.AssetTypeId).GreaterThan(0);"],
        "mapArgs": ["request.AssetTypeId", "request.IncludeInactive ?? false"],
        "mapCall": "new GetAssetTypeCustomFieldsRequest(assetTypeId, includeInactive)",
        "bind": "                int assetTypeId,\n                bool? includeInactive,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "DefineCustomField", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add a custom field to an asset type. Catalogue: Define custom fields - "
                   "type, required flag, range and dropdown options.",
        "capability": MANAGE,
        "verb": "Post", "route": "/types/{assetTypeId:int}/custom-fields",
        "command": [("int", "AssetTypeId"), ("string", "FieldName"), ("string", "DisplayLabel"),
                    ("string", "FieldType"), ("bool", "IsRequired"), ("decimal?", "MinValue"),
                    ("decimal?", "MaxValue"), ("string?", "ValidationRegex"), ("string?", "DefaultValue"),
                    ("int", "DisplayOrder"), ("IReadOnlyList<string>", "Options")],
        "request": [("string", "FieldName"), ("string", "DisplayLabel"), ("string", "FieldType"),
                    ("bool", "IsRequired"), ("decimal?", "MinValue"), ("decimal?", "MaxValue"),
                    ("string?", "ValidationRegex"), ("string?", "DefaultValue"), ("int?", "DisplayOrder"),
                    ("IReadOnlyList<string>?", "Options")],
        "response": [("int", "Id"), ("string", "FieldName"), ("string", "FieldType"),
                     ("IReadOnlyList<string>", "Options")],
        "responseSummary": "The new field definition.",
        "responseDocs": {
            "Id": "The new field.",
            "FieldName": "Unique within the asset type.",
            "FieldType": "One of Text, Number, Percentage, Date, Boolean, Dropdown (R2-26).",
            "Options": "The dropdown values, empty for every other type.",
        },
        "rules": [
            "RuleFor(x => x.FieldName).NotEmpty().MaximumLength(80);",
            "RuleFor(x => x.DisplayLabel).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.FieldType).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.ValidationRegex).MaximumLength(300);",
            "RuleFor(x => x.DefaultValue).MaximumLength(300);",
            "// CK_CustomFieldDefinition_Range says the same thing in the database.",
            "// Saying it here too turns a 500 into a message beside the field.",
            "RuleFor(x => x.MaxValue)",
            "    .GreaterThanOrEqualTo(x => x.MinValue!.Value)",
            "    .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);",
        ],
        "mapArgs": ["assetTypeId", "request.FieldName.Trim()", "request.DisplayLabel.Trim()",
                    "request.FieldType.Trim()", "request.IsRequired", "request.MinValue",
                    "request.MaxValue",
                    "string.IsNullOrWhiteSpace(request.ValidationRegex) ? null : request.ValidationRegex.Trim()",
                    "string.IsNullOrWhiteSpace(request.DefaultValue) ? null : request.DefaultValue.Trim()",
                    "request.DisplayOrder ?? 0", "request.Options ?? []"],
        "mapCall": "request, assetTypeId",
        "mapExtra": [("int", "assetTypeId")],
        "bind": "                int assetTypeId,\n                DefineCustomFieldRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => "
                  "$\"/api/v1/assets/types/{assetTypeId}/custom-fields/{response.Id}\")",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "UpdateCustomField", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit a custom field definition or retire it.",
        "capability": MANAGE,
        "verb": "Put", "route": "/custom-fields/{id:int}",
        "command": [("int", "Id"), ("string", "DisplayLabel"), ("bool", "IsRequired"),
                    ("decimal?", "MinValue"), ("decimal?", "MaxValue"), ("string?", "ValidationRegex"),
                    ("string?", "DefaultValue"), ("int", "DisplayOrder"), ("bool", "IsActive")],
        "request": [("string", "DisplayLabel"), ("bool", "IsRequired"), ("decimal?", "MinValue"),
                    ("decimal?", "MaxValue"), ("string?", "ValidationRegex"), ("string?", "DefaultValue"),
                    ("int?", "DisplayOrder"), ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "DisplayLabel"), ("bool", "IsActive")],
        "responseSummary": "The updated field definition.",
        "responseDocs": {
            "Id": "The field edited.",
            "DisplayLabel": "What the form shows beside the editor.",
            "IsActive": "Retiring hides the field from new assets; values already captured stay.",
        },
        "rules": [
            "RuleFor(x => x.DisplayLabel).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.ValidationRegex).MaximumLength(300);",
            "RuleFor(x => x.DefaultValue).MaximumLength(300);",
            "RuleFor(x => x.MaxValue)",
            "    .GreaterThanOrEqualTo(x => x.MinValue!.Value)",
            "    .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);",
        ],
        "mapArgs": ["id", "request.DisplayLabel.Trim()", "request.IsRequired", "request.MinValue",
                    "request.MaxValue",
                    "string.IsNullOrWhiteSpace(request.ValidationRegex) ? null : request.ValidationRegex.Trim()",
                    "string.IsNullOrWhiteSpace(request.DefaultValue) ? null : request.DefaultValue.Trim()",
                    "request.DisplayOrder ?? 0", "request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateCustomFieldRequest request,\n",
        "otherStatuses": ["Status404NotFound"],
    },
]

if __name__ == "__main__":
    main(SPECS)
