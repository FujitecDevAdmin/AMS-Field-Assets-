"""
Slice specifications for the Asset Register screen.

Catalogue screen: Asset Register — "Search, filter, page and export the
register - every asset the company owns, not only IT." Features: Register an
asset, Search filter and page, Export to Excel, Delete an asset, and the
Revision 3 additions that live on the asset record itself (class, make, model,
bulk quantity).

    python build/assets_slices_register.py
"""
from slices import main

NS = "AMS.Modules.Assets"
PROJECT = "AMS.Modules.Assets"

VIEW = "Capabilities.Assets.View"
MANAGE = "Capabilities.Assets.Manage"

# The columns a register row carries, shared by Register and Update so the two
# forms cannot drift apart.
CORE_COMMAND = [
    ("string", "AssetNumber"), ("string", "AssetName"), ("string?", "SerialNumber"),
    ("int", "AssetTypeId"), ("int?", "AssetClassId"), ("string?", "Make"), ("string?", "Model"),
    ("int", "AssetStatusId"), ("int?", "CurrentLocationId"), ("int?", "DepartmentId"),
    ("string?", "CostCenter"), ("DateOnly?", "AcquisitionDate"),
    ("bool", "IsBulk"), ("decimal", "Quantity"), ("string?", "UnitOfMeasure"),
    ("string?", "Remarks"),
]
CORE_REQUEST = [
    ("string", "AssetNumber"), ("string", "AssetName"), ("string?", "SerialNumber"),
    ("int", "AssetTypeId"), ("int?", "AssetClassId"), ("string?", "Make"), ("string?", "Model"),
    ("int", "AssetStatusId"), ("int?", "CurrentLocationId"), ("int?", "DepartmentId"),
    ("string?", "CostCenter"), ("DateOnly?", "AcquisitionDate"),
    ("bool?", "IsBulk"), ("decimal?", "Quantity"), ("string?", "UnitOfMeasure"),
    ("string?", "Remarks"),
]
CORE_MAP = [
    "request.AssetNumber.Trim()",
    "request.AssetName.Trim()",
    "string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim()",
    "request.AssetTypeId",
    "request.AssetClassId",
    "string.IsNullOrWhiteSpace(request.Make) ? null : request.Make.Trim()",
    "string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim()",
    "request.AssetStatusId",
    "request.CurrentLocationId",
    "request.DepartmentId",
    "string.IsNullOrWhiteSpace(request.CostCenter) ? null : request.CostCenter.Trim()",
    "request.AcquisitionDate",
    "request.IsBulk ?? false",
    # Quantity defaults to 1, exactly as DF_Asset_Quantity does. A unit asset
    # MUST be 1 - CK_Asset_UnitQuantityIsOne - so the form never has to ask.
    "request.Quantity ?? 1m",
    "string.IsNullOrWhiteSpace(request.UnitOfMeasure) ? null : request.UnitOfMeasure.Trim()",
    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()",
]
CORE_RULES = [
    "RuleFor(x => x.AssetNumber).NotEmpty().MaximumLength(40);",
    "RuleFor(x => x.AssetName).NotEmpty().MaximumLength(200);",
    "RuleFor(x => x.SerialNumber).MaximumLength(100);",
    "RuleFor(x => x.Make).MaximumLength(100);",
    "RuleFor(x => x.Model).MaximumLength(100);",
    "RuleFor(x => x.CostCenter).MaximumLength(40);",
    "RuleFor(x => x.UnitOfMeasure).MaximumLength(20);",
    "RuleFor(x => x.Remarks).MaximumLength(1000);",
    "RuleFor(x => x.AssetTypeId).GreaterThan(0);",
    "RuleFor(x => x.AssetStatusId).GreaterThan(0);",
    "RuleFor(x => x.AssetClassId).GreaterThan(0).When(x => x.AssetClassId.HasValue);",
    "// CK_Asset_QuantityPositive says the same thing in the database. Saying it",
    "// here too turns a 500 into a message beside the field.",
    "RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);",
]

SPECS = [
    {
        "name": "SearchAssets", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The register grid. Catalogue screen: Asset Register.",
        "capability": VIEW,
        "verb": "Get", "route": "",
        "command": [("string?", "Search"), ("int?", "AssetTypeId"), ("int?", "AssetClassId"),
                    ("int?", "AssetStatusId"), ("int?", "LocationId"), ("int?", "EmployeeId"),
                    ("bool?", "IsBulk"), ("bool", "IncludeDeleted"), ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Search"), ("int?", "AssetTypeId"), ("int?", "AssetClassId"),
                    ("int?", "AssetStatusId"), ("int?", "LocationId"), ("int?", "EmployeeId"),
                    ("bool?", "IsBulk"), ("bool?", "IncludeDeleted"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchAssetsResponse.Row>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "One page of the register, and how many match in total.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Assets matching the filter, ignoring paging.",
        },
        "rules": [
            "RuleFor(x => x.Search).MaximumLength(100);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "// An unbounded page over 7,413 rows is a review-blocker (02 section 8).",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim()",
                    "request.AssetTypeId", "request.AssetClassId", "request.AssetStatusId",
                    "request.LocationId", "request.EmployeeId", "request.IsBulk",
                    "request.IncludeDeleted ?? false", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchAssetsRequest request,\n",
    },
    {
        "name": "RegisterAsset", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Register an asset. Catalogue: Register an asset.",
        "capability": MANAGE,
        "verb": "Post", "route": "",
        "command": CORE_COMMAND,
        "request": CORE_REQUEST,
        "response": [("int", "Id"), ("string", "AssetNumber"), ("string", "AssetName")],
        "responseSummary": "The new asset.",
        "responseDocs": {
            "Id": "The new asset.",
            "AssetNumber": "Unique, enforced by UX_Asset_Number.",
            "AssetName": "As stored.",
        },
        "rules": CORE_RULES,
        "mapArgs": CORE_MAP,
        "mapCall": "request",
        "bind": "                RegisterAssetRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/assets/{response.Id}\")",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "UpdateAsset", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit an asset already on the register.",
        "capability": MANAGE,
        "verb": "Put", "route": "/{id:int}",
        "command": [("int", "Id")] + CORE_COMMAND,
        "request": CORE_REQUEST,
        "response": [("int", "Id"), ("string", "AssetNumber"), ("string", "AssetName")],
        "responseSummary": "The updated asset.",
        "responseDocs": {
            "Id": "The asset edited.",
            "AssetNumber": "Unique, enforced by UX_Asset_Number.",
            "AssetName": "As stored.",
        },
        "rules": CORE_RULES,
        "mapArgs": ["id"] + CORE_MAP,
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateAssetRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "DeleteAsset", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Remove an asset from the register. Catalogue: Delete an asset - "
                   "marked as deleted, never physically removed, so history keeps its meaning.",
        "capability": MANAGE,
        "verb": "Delete", "route": "/{id:int}",
        "command": [("int", "Id"), ("string?", "Reason")],
        "request": [("string?", "Reason")],
        "response": [("int", "Id"), ("bool", "IsDeleted")],
        "responseSummary": "The asset, now marked deleted.",
        "responseDocs": {
            "Id": "The asset removed.",
            "IsDeleted": "Always true. The row and its timeline stay.",
        },
        "rules": ["RuleFor(x => x.Reason).MaximumLength(300);"],
        "mapArgs": ["id",
                    "string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                DeleteAssetRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
