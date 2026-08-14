"""
Slice specifications for the Verification module.

Catalogue screens: Verification Cycles, the mobile capture, and the exception
report. This is the handbook's "asset audit" done properly — QR scan, GPS,
photo and a working-condition judgement, captured offline on a phone and
synced.

Two things shape it:

  * A unit asset is SIGHTED once per cycle; a bulk line is COUNTED wherever it
    is held. R3 split the unique index for exactly that reason, and the two
    kinds of row are told apart by IsBulkCount.

  * The phone generates ClientCaptureId at capture and sends the same value on
    every retry (R2-21). A retry and a genuine conflict both arrive as SQL
    2601, and they deserve different words: one is "you already sent this", the
    other is "somebody else got there first".

    python build/verification_slices.py
"""
from slices import main

NS = "AMS.Modules.Verification"
PROJECT = "AMS.Modules.Verification"

RUN = "Capabilities.Verification.Run"
VIEW = "Capabilities.Verification.View"
MANAGE = "Capabilities.Verification.Manage"

SPECS = [
    {
        "name": "SearchVerificationCycles", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The verification cycles. Catalogue: Verification Cycles.",
        "capability": VIEW,
        "verb": "Get", "route": "/cycles",
        "command": [("bool", "ActiveOnly")],
        "request": [("bool?", "ActiveOnly")],
        "response": [("IReadOnlyList<SearchVerificationCyclesResponse.Row>", "Rows")],
        "responseSummary": "The cycles, newest first.",
        "responseDocs": {"Rows": "Each with how much of it has been done."},
        "rules": [],
        "mapArgs": ["request.ActiveOnly ?? false"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchVerificationCyclesRequest request,\n",
    },
    {
        "name": "OpenVerificationCycle", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Start a verification round. Catalogue: Verification Cycles.",
        "capability": MANAGE,
        "verb": "Post", "route": "/cycles",
        "command": [("string", "CycleName"), ("DateOnly", "StartDate"), ("DateOnly?", "EndDate")],
        "request": [("string", "CycleName"), ("DateOnly?", "StartDate"), ("DateOnly?", "EndDate")],
        "response": [("int", "Id"), ("string", "CycleName"), ("DateOnly", "StartDate")],
        "responseSummary": "The cycle, open.",
        "responseDocs": {
            "Id": "The cycle.",
            "CycleName": "What it is called.",
            "StartDate": "When counting began.",
        },
        "rules": ["RuleFor(x => x.CycleName).NotEmpty().MaximumLength(120);"],
        "mapArgs": ["request.CycleName.Trim()", "request.StartDate ?? default", "request.EndDate"],
        "mapCall": "request",
        "bind": "                OpenVerificationCycleRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "CloseVerificationCycle", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Finish a verification round. Catalogue: Verification Cycles.",
        "capability": MANAGE,
        "verb": "Post", "route": "/cycles/{id:int}/closure",
        "command": [("int", "Id")],
        "request": [],
        "response": [("int", "Id"), ("int", "VerifiedCount"), ("int", "ExceptionCount"),
                     ("DateTime", "ClosedOnUtc")],
        "responseSummary": "The cycle, closed, and what it found.",
        "responseDocs": {
            "Id": "The cycle.",
            "VerifiedCount": "How many rows were recorded.",
            "ExceptionCount": "How many of those were not Good.",
            "ClosedOnUtc": "When it was closed.",
        },
        "rules": [],
        "mapArgs": ["id"],
        "mapCall": "new CloseVerificationCycleRequest(), id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SubmitVerification", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Record a sighting or a bulk count. Catalogue: the mobile capture.",
        "capability": RUN,
        "verb": "Post", "route": "/verifications",
        "command": [("int", "AssetId"), ("Guid?", "ClientCaptureId"), ("bool", "IsBulkCount"),
                    ("decimal?", "CountedQuantity"), ("decimal?", "ExpectedQuantitySnapshot"),
                    ("string?", "ScannedQrValue"), ("string", "WorkingCondition"),
                    ("bool", "SerialVerified"), ("decimal?", "GpsLatitude"),
                    ("decimal?", "GpsLongitude"), ("string?", "PhotoPath"),
                    ("int?", "LocationId"), ("int?", "HolderEmployeeId"),
                    ("DateTime?", "VerifiedOnUtc"), ("string?", "Remarks")],
        "request": [("int", "AssetId"), ("Guid?", "ClientCaptureId"), ("bool?", "IsBulkCount"),
                    ("decimal?", "CountedQuantity"), ("decimal?", "ExpectedQuantitySnapshot"),
                    ("string?", "ScannedQrValue"), ("string?", "WorkingCondition"),
                    ("bool?", "SerialVerified"), ("decimal?", "GpsLatitude"),
                    ("decimal?", "GpsLongitude"), ("string?", "PhotoPath"),
                    ("int?", "LocationId"), ("int?", "HolderEmployeeId"),
                    ("DateTime?", "VerifiedOnUtc"), ("string?", "Remarks")],
        "response": [("int", "Id"), ("int", "AssetId"), ("string", "AssetNumber"),
                     ("string", "WorkingCondition"), ("bool", "HasQrMismatch"),
                     ("decimal?", "Variance"), ("bool", "WasAlreadyRecorded")],
        "responseSummary": "The verification, as recorded.",
        "responseDocs": {
            "Id": "The row.",
            "AssetId": "What was verified.",
            "AssetNumber": "For a message a person has to read.",
            "WorkingCondition": "What it was found in.",
            "HasQrMismatch": "Whether the scanned tag belonged to a different asset.",
            "Variance": "Counted minus expected, on a bulk count. Null on a sighting.",
            "WasAlreadyRecorded": "True when this device had already sent this capture. "
                                  "The answer is the row it sent, not a second one.",
        },
        "rules": [
            "RuleFor(x => x.AssetId).GreaterThan(0);",
            "RuleFor(x => x.WorkingCondition).MaximumLength(20);",
            "RuleFor(x => x.ScannedQrValue).MaximumLength(200);",
            "RuleFor(x => x.PhotoPath).MaximumLength(400);",
            "RuleFor(x => x.Remarks).MaximumLength(500);",
            "RuleFor(x => x.CountedQuantity).GreaterThanOrEqualTo(0)"
            ".When(x => x.CountedQuantity.HasValue);",
            "RuleFor(x => x.GpsLatitude).InclusiveBetween(-90, 90)"
            ".When(x => x.GpsLatitude.HasValue);",
            "RuleFor(x => x.GpsLongitude).InclusiveBetween(-180, 180)"
            ".When(x => x.GpsLongitude.HasValue);",
        ],
        "mapArgs": ["request.AssetId", "request.ClientCaptureId", "request.IsBulkCount ?? false",
                    "request.CountedQuantity", "request.ExpectedQuantitySnapshot",
                    "string.IsNullOrWhiteSpace(request.ScannedQrValue) ? null : request.ScannedQrValue.Trim()",
                    "string.IsNullOrWhiteSpace(request.WorkingCondition) ? WorkingCondition.Good : request.WorkingCondition.Trim()",
                    "request.SerialVerified ?? false",
                    "request.GpsLatitude", "request.GpsLongitude",
                    "string.IsNullOrWhiteSpace(request.PhotoPath) ? null : request.PhotoPath.Trim()",
                    "request.LocationId", "request.HolderEmployeeId", "request.VerifiedOnUtc",
                    "string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim()"],
        "mapCall": "request",
        "bind": "                SubmitVerificationRequest request,\n",
        "successStatus": "Status201Created",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SearchVerifications", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What was found, and what was not. Catalogue: the exception report.",
        "capability": VIEW,
        "verb": "Get", "route": "/verifications",
        "command": [("int?", "CycleId"), ("int?", "LocationId"), ("string?", "WorkingCondition"),
                    ("bool", "ExceptionsOnly"), ("bool", "MismatchesOnly"),
                    ("int", "Skip"), ("int", "Take")],
        "request": [("int?", "CycleId"), ("int?", "LocationId"), ("string?", "WorkingCondition"),
                    ("bool?", "ExceptionsOnly"), ("bool?", "MismatchesOnly"),
                    ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchVerificationsResponse.Row>", "Rows"),
                     ("int", "TotalCount"), ("int", "ExceptionCount")],
        "responseSummary": "One page of results, worst first.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Rows matching the filter.",
            "ExceptionCount": "How many of those were not Good.",
        },
        "rules": [
            "RuleFor(x => x.WorkingCondition).MaximumLength(20);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.CycleId", "request.LocationId",
                    "string.IsNullOrWhiteSpace(request.WorkingCondition) ? null : request.WorkingCondition.Trim()",
                    "request.ExceptionsOnly ?? false", "request.MismatchesOnly ?? false",
                    "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchVerificationsRequest request,\n",
    },
]

if __name__ == "__main__":
    main(SPECS)
