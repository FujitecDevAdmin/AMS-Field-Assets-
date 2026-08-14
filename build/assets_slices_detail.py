"""
Slice specifications for the Asset Detail and Timeline screen.

Catalogue screen: Asset Detail & Timeline — "The full record, its details,
custom fields and every event in order." Features: Asset detail and timeline,
Hardware details, Software details, Purchase and warranty, Calibration and
instrument details, Vehicle details, Fill custom fields, Book values mirrored
from SAP.

    python build/assets_slices_detail.py
"""
from slices import main

NS = "AMS.Modules.Assets"
PROJECT = "AMS.Modules.Assets"

VIEW = "Capabilities.Assets.View"
MANAGE = "Capabilities.Assets.Manage"

SPECS = [
    {
        "name": "GetAsset", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "One asset in full. Catalogue screen: Asset Detail and Timeline.",
        "capability": VIEW,
        "verb": "Get", "route": "/{id:int}",
        "command": [("int", "Id")],
        "request": [("int", "Id")],
        "response": [("GetAssetResponse.Core", "Asset"),
                     ("GetAssetResponse.Hardware?", "HardwareDetail"),
                     ("GetAssetResponse.Software?", "SoftwareDetail"),
                     ("GetAssetResponse.Purchase?", "PurchaseDetail"),
                     ("GetAssetResponse.Vehicle?", "VehicleDetail"),
                     ("GetAssetResponse.Instrument?", "InstrumentDetail"),
                     ("GetAssetResponse.Finance?", "Finance"),
                     ("IReadOnlyList<GetAssetResponse.CustomValue>", "CustomValues")],
        "responseSummary": "Everything the detail screen renders, in one round trip.",
        "responseDocs": {
            "Asset": "The register row itself.",
            "HardwareDetail": "Null unless the asset type tracks hardware.",
            "SoftwareDetail": "Null unless the asset type tracks software.",
            "PurchaseDetail": "Null until somebody records a purchase.",
            "VehicleDetail": "Null unless the asset type tracks vehicles.",
            "InstrumentDetail": "Null unless the asset type tracks calibration.",
            "Finance": "Null unless the caller may read book values AND SAP has synced some.",
            "CustomValues": "One entry per field defined for the asset's type, value included.",
        },
        "rules": ["RuleFor(x => x.Id).GreaterThan(0);"],
        "mapArgs": ["request.Id"],
        "mapCall": "new GetAssetRequest(id)",
        "bind": "                int id,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "GetAssetTimeline", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "Everything that has happened to one asset, newest first.",
        "capability": VIEW,
        "verb": "Get", "route": "/{id:int}/timeline",
        "command": [("int", "AssetId"), ("int", "Skip"), ("int", "Take")],
        "request": [("int", "AssetId"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<GetAssetTimelineResponse.Row>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "One page of the timeline, and how many entries there are.",
        "responseDocs": {
            "Rows": "The page, newest first.",
            "TotalCount": "Entries against this asset, ignoring paging.",
        },
        "rules": [
            "RuleFor(x => x.AssetId).GreaterThan(0);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.AssetId", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "new GetAssetTimelineRequest(id, skip, take)",
        "bind": "                int id,\n                int? skip,\n                int? take,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "SaveAssetDetails", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Record the 1:1 detail that applies to this asset's type — hardware, "
                   "software, purchase, vehicle or calibration.",
        "capability": MANAGE,
        "verb": "Put", "route": "/{id:int}/details",
        "command": [("int", "AssetId"),
                    ("SaveAssetDetailsCommand.HardwareInput?", "Hardware"),
                    ("SaveAssetDetailsCommand.SoftwareInput?", "Software"),
                    ("SaveAssetDetailsCommand.PurchaseInput?", "Purchase"),
                    ("SaveAssetDetailsCommand.VehicleInput?", "Vehicle"),
                    ("SaveAssetDetailsCommand.InstrumentInput?", "Instrument")],
        "request": [("SaveAssetDetailsCommand.HardwareInput?", "Hardware"),
                    ("SaveAssetDetailsCommand.SoftwareInput?", "Software"),
                    ("SaveAssetDetailsCommand.PurchaseInput?", "Purchase"),
                    ("SaveAssetDetailsCommand.VehicleInput?", "Vehicle"),
                    ("SaveAssetDetailsCommand.InstrumentInput?", "Instrument")],
        "response": [("int", "AssetId"), ("IReadOnlyList<string>", "Saved")],
        "responseSummary": "Which detail records were written.",
        "responseDocs": {
            "AssetId": "The asset.",
            "Saved": "The detail kinds saved, so the screen can confirm what it did.",
        },
        "rules": [],
        "mapArgs": ["id", "request.Hardware", "request.Software", "request.Purchase",
                    "request.Vehicle", "request.Instrument"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                SaveAssetDetailsRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SetAssetCustomValues", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Fill in the custom fields defined for this asset's type. "
                   "Catalogue: Fill custom fields.",
        "capability": MANAGE,
        "verb": "Put", "route": "/{id:int}/custom-values",
        "command": [("int", "AssetId"),
                    ("IReadOnlyList<SetAssetCustomValuesCommand.Entry>", "Values")],
        "request": [("IReadOnlyList<SetAssetCustomValuesCommand.Entry>?", "Values")],
        "response": [("int", "AssetId"), ("int", "SavedCount")],
        "responseSummary": "How many values were written.",
        "responseDocs": {
            "AssetId": "The asset.",
            "SavedCount": "Values stored, after blanks were cleared.",
        },
        "rules": [],
        "mapArgs": ["id", "request.Values ?? []"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                SetAssetCustomValuesRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
