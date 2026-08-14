"""
Slice specifications for the Movements module.

Catalogue screens: Despatch, Despatch Batch, GRN Queue. Six features, from
"Despatch an asset" to "Goods receipt at head office".

The rule underneath all of it, from the design script's own note: an asset in
transit belongs to NEITHER branch. CurrentLocationId changes on RECEIPT and
never on despatch, because marking it as arrived on despatch makes it findable
somewhere it is not.

    python build/movements_slices.py
"""
from slices import main

NS = "AMS.Modules.Movements"
PROJECT = "AMS.Modules.Movements"

VIEW = "Capabilities.Movements.View"
MANAGE = "Capabilities.Movements.Manage"
RECEIVE = "Capabilities.Movements.Receive"

SPECS = [
    {
        "name": "SearchMovements", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "Shipments and where they have got to. Catalogue screen: Despatch.",
        "capability": VIEW,
        "verb": "Get", "route": "",
        "command": [("string?", "Status"), ("int?", "AssetId"), ("int?", "FromLocationId"),
                    ("int?", "ToLocationId"), ("int?", "MovementBatchId"),
                    ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Status"), ("int?", "AssetId"), ("int?", "FromLocationId"),
                    ("int?", "ToLocationId"), ("int?", "MovementBatchId"),
                    ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchMovementsResponse.Row>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "One page of shipments, newest first.",
        "responseDocs": {"Rows": "The page.", "TotalCount": "Shipments matching the filter."},
        "rules": [
            "RuleFor(x => x.Status).MaximumLength(20);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim()",
                    "request.AssetId", "request.FromLocationId", "request.ToLocationId",
                    "request.MovementBatchId", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchMovementsRequest request,\n",
    },
    {
        "name": "DespatchAsset", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Send one asset to another branch or to head office. Catalogue: "
                   "Despatch an asset, with courier, tracking and challan.",
        "capability": MANAGE,
        "verb": "Post", "route": "",
        "command": [("int", "AssetId"), ("string", "MovementType"), ("int", "FromLocationId"),
                    ("int", "ToLocationId"), ("decimal", "Quantity"), ("int?", "HandoverId"),
                    ("string?", "CourierName"), ("string?", "TrackingNumber"),
                    ("string?", "ChallanNumber"), ("string?", "InvoiceNumber"),
                    ("DateOnly?", "InvoiceDate"), ("string?", "Remarks")],
        "request": [("int", "AssetId"), ("string", "MovementType"), ("int", "FromLocationId"),
                    ("int", "ToLocationId"), ("decimal?", "Quantity"), ("int?", "HandoverId"),
                    ("string?", "CourierName"), ("string?", "TrackingNumber"),
                    ("string?", "ChallanNumber"), ("string?", "InvoiceNumber"),
                    ("DateOnly?", "InvoiceDate"), ("string?", "Remarks")],
        "response": [("int", "Id"), ("int", "AssetId"), ("string", "Status")],
        "responseSummary": "The shipment, in transit.",
        "responseDocs": {
            "Id": "The shipment.",
            "AssetId": "What is travelling.",
            "Status": "Always InTransit. The asset's branch does not change until it arrives.",
        },
        "rules": [
            "RuleFor(x => x.AssetId).GreaterThan(0);",
            "RuleFor(x => x.MovementType).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.FromLocationId).GreaterThan(0);",
            "RuleFor(x => x.ToLocationId).GreaterThan(0);",
            "// CK_AssetMovement_DifferentBranches says the same thing. Saying it",
            "// here turns a 500 into a message beside the field.",
            "RuleFor(x => x.ToLocationId).NotEqual(x => x.FromLocationId)",
            "    .WithMessage(\"An asset cannot be sent to the branch it is leaving.\");",
            "RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);",
            "RuleFor(x => x.CourierName).MaximumLength(100);",
            "RuleFor(x => x.TrackingNumber).MaximumLength(80);",
            "RuleFor(x => x.ChallanNumber).MaximumLength(80);",
            "RuleFor(x => x.InvoiceNumber).MaximumLength(80);",
            "RuleFor(x => x.Remarks).MaximumLength(500);",
        ],
        "mapArgs": ["request.AssetId", "request.MovementType.Trim()", "request.FromLocationId",
                    "request.ToLocationId", "request.Quantity ?? 1m", "request.HandoverId",
                    "string.IsNullOrWhiteSpace(request.CourierName) ? null : request.CourierName.Trim()",
                    "string.IsNullOrWhiteSpace(request.TrackingNumber) ? null : request.TrackingNumber.Trim()",
                    "string.IsNullOrWhiteSpace(request.ChallanNumber) ? null : request.ChallanNumber.Trim()",
                    "string.IsNullOrWhiteSpace(request.InvoiceNumber) ? null : request.InvoiceNumber.Trim()",
                    "request.InvoiceDate",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()"],
        "mapCall": "request",
        "bind": "                DespatchAssetRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/movements/{response.Id}\")",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "DespatchBatch", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Send several assets on one consignment. Catalogue: Despatch several "
                   "assets at once - one invoice and courier, every asset gets its own "
                   "tracking row.",
        "capability": MANAGE,
        "verb": "Post", "route": "/batches",
        "command": [("string", "MovementType"), ("int", "FromLocationId"), ("int", "ToLocationId"),
                    ("string", "InvoiceNumber"), ("DateOnly", "InvoiceDate"),
                    ("string", "CourierName"), ("string?", "TrackingNumber"),
                    ("string?", "ChallanNumber"), ("string", "Remarks"),
                    ("IReadOnlyList<int>", "AssetIds")],
        "request": [("string", "MovementType"), ("int", "FromLocationId"), ("int", "ToLocationId"),
                    ("string", "InvoiceNumber"), ("DateOnly", "InvoiceDate"),
                    ("string", "CourierName"), ("string?", "TrackingNumber"),
                    ("string?", "ChallanNumber"), ("string", "Remarks"),
                    ("IReadOnlyList<int>?", "AssetIds")],
        "response": [("int", "Id"), ("string", "BatchNumber"), ("int", "ItemCount")],
        "responseSummary": "The consignment.",
        "responseDocs": {
            "Id": "The batch.",
            "BatchNumber": "From MovementBatchNumberSequence. Unique.",
            "ItemCount": "How many assets went on it.",
        },
        "rules": [
            "RuleFor(x => x.MovementType).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.FromLocationId).GreaterThan(0);",
            "RuleFor(x => x.ToLocationId).GreaterThan(0);",
            "RuleFor(x => x.ToLocationId).NotEqual(x => x.FromLocationId)",
            "    .WithMessage(\"A consignment cannot be sent to the branch it is leaving.\");",
            "// Held once on the consignment rather than repeated on each asset:",
            "// three rows carrying one invoice number is three chances to edit one.",
            "RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(80);",
            "RuleFor(x => x.CourierName).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.TrackingNumber).MaximumLength(80);",
            "RuleFor(x => x.ChallanNumber).MaximumLength(80);",
            "RuleFor(x => x.Remarks).NotEmpty().MaximumLength(500);",
        ],
        "mapArgs": ["request.MovementType.Trim()", "request.FromLocationId", "request.ToLocationId",
                    "request.InvoiceNumber.Trim()", "request.InvoiceDate",
                    "request.CourierName.Trim()",
                    "string.IsNullOrWhiteSpace(request.TrackingNumber) ? null : request.TrackingNumber.Trim()",
                    "string.IsNullOrWhiteSpace(request.ChallanNumber) ? null : request.ChallanNumber.Trim()",
                    "request.Remarks.Trim()", "request.AssetIds ?? []"],
        "mapCall": "request",
        "bind": "                DespatchBatchRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/movements/batches/{response.Id}\")",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "GetGrnQueue", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "Pending receipts at the destination. Catalogue screen: GRN Queue.",
        "capability": VIEW,
        "verb": "Get", "route": "/grn-queue",
        "command": [("int?", "ToLocationId"), ("int", "Skip"), ("int", "Take")],
        "request": [("int?", "ToLocationId"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<GetGrnQueueResponse.Row>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "Everything in transit to this branch, oldest first.",
        "responseDocs": {
            "Rows": "The queue.",
            "TotalCount": "Shipments still in transit.",
        },
        "rules": [
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.ToLocationId", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] GetGrnQueueRequest request,\n",
    },
    {
        "name": "ReceiveMovement", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Confirm arrival at the destination. Catalogue: Receive at the "
                   "destination, and Goods receipt at head office.",
        "capability": RECEIVE,
        "verb": "Post", "route": "/{id:int}/receive",
        "command": [("int", "Id"), ("string?", "ReceiptRemarks")],
        "request": [("string?", "ReceiptRemarks")],
        "response": [("int", "Id"), ("int", "AssetId"), ("int", "ToLocationId"),
                     ("bool", "BatchComplete")],
        "responseSummary": "The received shipment.",
        "responseDocs": {
            "Id": "The shipment.",
            "AssetId": "The asset, now at the receiving branch.",
            "ToLocationId": "Where it arrived — and only now where the asset says it is.",
            "BatchComplete": "True when this was the last outstanding item on its consignment.",
        },
        "rules": ["RuleFor(x => x.ReceiptRemarks).MaximumLength(500);"],
        "mapArgs": ["id",
                    "string.IsNullOrWhiteSpace(request.ReceiptRemarks) ? null : request.ReceiptRemarks.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                ReceiveMovementRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
