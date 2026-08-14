"""
Slice specifications for the Transfers module.

Catalogue screen: Transfer Requests. Five features — raise, approve or reject,
complete, cancel, and the SAP queue the System drains.

A transfer is the APPROVAL and the accounting consequence. The physical
shipment it may cause is a [Movements].[AssetMovement] linked by id, and that
moves the asset again on arrival — which is why completing a Branch transfer
records the accounting fact and does not pretend the thing has travelled.

    python build/transfers_slices.py
"""
from slices import main

NS = "AMS.Modules.Transfers"
PROJECT = "AMS.Modules.Transfers"

VIEW = "Capabilities.Transfers.View"
REQUEST = "Capabilities.Transfers.Request"
APPROVE = "Capabilities.Transfers.Approve"
COMPLETE = "Capabilities.Transfers.Complete"

SPECS = [
    {
        "name": "SearchTransferRequests", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The transfer queue and its SAP status. Catalogue screen: Transfer Requests.",
        "capability": VIEW,
        "verb": "Get", "route": "",
        "command": [("string?", "Status"), ("string?", "TransferType"), ("int?", "AssetId"),
                    ("string?", "SapSyncStatus"), ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Status"), ("string?", "TransferType"), ("int?", "AssetId"),
                    ("string?", "SapSyncStatus"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchTransferRequestsResponse.Row>", "Rows"),
                     ("int", "TotalCount")],
        "responseSummary": "One page of transfer requests, newest first.",
        "responseDocs": {"Rows": "The page.", "TotalCount": "Requests matching the filter."},
        "rules": [
            "RuleFor(x => x.Status).MaximumLength(20);",
            "RuleFor(x => x.TransferType).MaximumLength(20);",
            "RuleFor(x => x.SapSyncStatus).MaximumLength(20);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim()",
                    "string.IsNullOrWhiteSpace(request.TransferType) ? null : request.TransferType.Trim()",
                    "request.AssetId",
                    "string.IsNullOrWhiteSpace(request.SapSyncStatus) ? null : request.SapSyncStatus.Trim()",
                    "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchTransferRequestsRequest request,\n",
    },
    {
        "name": "RaiseTransfer", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Raise a transfer. Catalogue: by employee, department, branch or cost centre.",
        "capability": REQUEST,
        "verb": "Post", "route": "",
        "command": [("int", "AssetId"), ("string", "TransferType"), ("int?", "ToEmployeeId"),
                    ("int?", "ToDepartmentId"), ("int?", "ToLocationId"),
                    ("string?", "ToCostCenter"), ("string?", "Remarks")],
        "request": [("int", "AssetId"), ("string", "TransferType"), ("int?", "ToEmployeeId"),
                    ("int?", "ToDepartmentId"), ("int?", "ToLocationId"),
                    ("string?", "ToCostCenter"), ("string?", "Remarks")],
        "response": [("int", "Id"), ("string", "TransferType"), ("string", "Status")],
        "responseSummary": "The new request, Pending.",
        "responseDocs": {
            "Id": "The request.",
            "TransferType": "Employee, Department, Branch or CostCenter.",
            "Status": "Always Pending. Somebody else decides it.",
        },
        "rules": [
            "RuleFor(x => x.AssetId).GreaterThan(0);",
            "RuleFor(x => x.TransferType).NotEmpty().MaximumLength(20);",
            "RuleFor(x => x.ToCostCenter).MaximumLength(40);",
            "RuleFor(x => x.Remarks).MaximumLength(500);",
        ],
        "mapArgs": ["request.AssetId", "request.TransferType.Trim()", "request.ToEmployeeId",
                    "request.ToDepartmentId", "request.ToLocationId",
                    "string.IsNullOrWhiteSpace(request.ToCostCenter) ? null : request.ToCostCenter.Trim()",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()"],
        "mapCall": "request",
        "bind": "                RaiseTransferRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/transfers/{response.Id}\")",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "DecideTransfer", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Approve or reject a transfer. Catalogue: with a remark.",
        "capability": APPROVE,
        "verb": "Post", "route": "/{id:int}/decision",
        "command": [("int", "Id"), ("bool", "Approved"), ("string?", "Remarks")],
        "request": [("bool", "Approved"), ("string?", "Remarks")],
        "response": [("int", "Id"), ("string", "Status")],
        "responseSummary": "The decided request.",
        "responseDocs": {
            "Id": "The request.",
            "Status": "Approved or Rejected. Approved does NOT mean applied — completing does that.",
        },
        "rules": ["RuleFor(x => x.Remarks).MaximumLength(500);"],
        "mapArgs": ["id", "request.Approved",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                DecideTransferRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "CompleteTransfer", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Apply an approved transfer. Catalogue: applies the change and queues it "
                   "to SAP where the accounting system needs to know.",
        "capability": COMPLETE,
        "verb": "Post", "route": "/{id:int}/complete",
        "command": [("int", "Id"), ("int?", "MovementId")],
        "request": [("int?", "MovementId")],
        "response": [("int", "Id"), ("string", "Status"), ("string", "SapSyncStatus")],
        "responseSummary": "The completed transfer.",
        "responseDocs": {
            "Id": "The request.",
            "Status": "Completed. The register now says what the transfer asked for.",
            "SapSyncStatus": "Pending when SAP needs telling, NotRequired when it does not.",
        },
        "rules": ["RuleFor(x => x.MovementId).GreaterThan(0).When(x => x.MovementId.HasValue);"],
        "mapArgs": ["id", "request.MovementId"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                CompleteTransferRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "CancelTransfer", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Withdraw a transfer before it is completed. Catalogue: Cancel.",
        "capability": APPROVE,
        "verb": "Post", "route": "/{id:int}/cancel",
        "command": [("int", "Id"), ("string", "Reason")],
        "request": [("string", "Reason")],
        "response": [("int", "Id"), ("string", "Status")],
        "responseSummary": "The cancelled request. The row stays — it is the record it was asked for.",
        "responseDocs": {"Id": "The request.", "Status": "Cancelled."},
        "rules": ["RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);"],
        "mapArgs": ["id", "request.Reason.Trim()"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                CancelTransferRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
