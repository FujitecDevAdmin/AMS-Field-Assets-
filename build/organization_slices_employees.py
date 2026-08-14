"""
Slice specifications for the Organization module's Employee Directory screen.

Catalogue features 4 and 5: Employee directory, Reporting manager.

Employee is the first system-versioned table the application writes, so its
ETag is the ConcurrencyStamp (R2-22) rather than a rowversion. Importing
employees from a spreadsheet is catalogue feature 12 and belongs to the Data
Import module, not here.

    python build/organization_slices_employees.py
"""
from slices import main

NS = "AMS.Modules.Organization"
PROJECT = "AMS.Modules.Organization"
MANAGE = "Capabilities.Organization.EmployeeManage"
VIEW = "Capabilities.Organization.EmployeeView"

SPECS = [
    {
        "name": "SearchEmployees", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The employee directory, filtered and paged.",
        "capability": VIEW,
        "verb": "Get", "route": "/employees",
        "command": [("string?", "Search"), ("int?", "DepartmentId"), ("int?", "LocationId"),
                    ("bool?", "IsActive"), ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Search"), ("int?", "DepartmentId"), ("int?", "LocationId"),
                    ("bool?", "IsActive"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchEmployeesResponse.Row>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "One page of employees, and how many match in total.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Employees matching the filter, ignoring paging.",
        },
        "rules": [
            "RuleFor(x => x.Search).MaximumLength(100);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "// An unbounded employee list is a review-blocker (02 §8).",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.Search?.Trim()", "request.DepartmentId", "request.LocationId",
                    "request.IsActive", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchEmployeesRequest request,\n",
    },
    {
        "name": "GetEmployee", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "One employee, as the directory form edits them.",
        "capability": VIEW,
        "verb": "Get", "route": "/employees/{employeeId:int}",
        "command": [("int", "EmployeeId")],
        "request": [("int", "EmployeeId")],
        "response": [("int", "Id"), ("string", "EmployeeCode"), ("string", "FullName"),
                     ("string?", "Email"), ("string?", "Phone"), ("int?", "DepartmentId"),
                     ("string?", "DepartmentName"), ("int?", "LocationId"), ("string?", "LocationName"),
                     ("int?", "ReportingManagerId"), ("string?", "ReportingManagerName"),
                     ("bool", "IsActive"), ("string", "ETag")],
        "responseSummary": "Everything the Employee Directory form shows for one person.",
        "responseDocs": {
            "ETag": "The ConcurrencyStamp. Employee is system-versioned, so the token is "
                    "ConcurrencyStamp and NOT a rowversion (R2-22). A mismatch is a 412.",
            "DepartmentName": "Denormalised for display; null when DepartmentId is.",
            "LocationName": "Denormalised for display; null when LocationId is.",
            "ReportingManagerName": "Denormalised for display; null when the employee reports to nobody.",
        },
        "rules": ["RuleFor(x => x.EmployeeId).GreaterThan(0);"],
        "mapArgs": ["request.EmployeeId"],
        "mapCall": "new GetEmployeeRequest(employeeId)",
        "bind": "                int employeeId,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "CreateEmployee", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add somebody to the directory. Catalogue: Employee directory, "
                   "Reporting manager.",
        "capability": MANAGE,
        "verb": "Post", "route": "/employees",
        "command": [("string", "EmployeeCode"), ("string", "FullName"), ("string?", "Email"),
                    ("string?", "Phone"), ("int?", "DepartmentId"), ("int?", "LocationId"),
                    ("int?", "ReportingManagerId")],
        "request": [("string", "EmployeeCode"), ("string", "FullName"), ("string?", "Email"),
                    ("string?", "Phone"), ("int?", "DepartmentId"), ("int?", "LocationId"),
                    ("int?", "ReportingManagerId")],
        "response": [("int", "Id"), ("string", "EmployeeCode"), ("string", "FullName"),
                     ("string", "ETag")],
        "responseSummary": "The new employee.",
        "responseDocs": {
            "Id": "The new employee.",
            "EmployeeCode": "Unique, upper-cased.",
            "FullName": "As stored, trimmed.",
            "ETag": "The ConcurrencyStamp (R2-22).",
        },
        "rules": [
            "RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);",
            "RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.Email).MaximumLength(256).EmailAddress()",
            "    .When(x => !string.IsNullOrWhiteSpace(x.Email));",
            "RuleFor(x => x.Phone).MaximumLength(40);",
            "RuleFor(x => x.DepartmentId).GreaterThan(0).When(x => x.DepartmentId.HasValue);",
            "RuleFor(x => x.LocationId).GreaterThan(0).When(x => x.LocationId.HasValue);",
            "RuleFor(x => x.ReportingManagerId).GreaterThan(0).When(x => x.ReportingManagerId.HasValue);",
        ],
        "mapArgs": ["request.EmployeeCode.Trim().ToUpperInvariant()", "request.FullName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim()",
                    "string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()",
                    "request.DepartmentId", "request.LocationId", "request.ReportingManagerId"],
        "mapCall": "request",
        "bind": "                CreateEmployeeRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/organization/employees/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateEmployee", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit an employee. Catalogue: Employee directory, Reporting manager.",
        "capability": MANAGE,
        "verb": "Put", "route": "/employees/{employeeId:int}",
        "command": [("int", "EmployeeId"), ("string", "EmployeeCode"), ("string", "FullName"),
                    ("string?", "Email"), ("string?", "Phone"), ("int?", "DepartmentId"),
                    ("int?", "LocationId"), ("int?", "ReportingManagerId"), ("string", "ETag")],
        "request": [("string", "EmployeeCode"), ("string", "FullName"), ("string?", "Email"),
                    ("string?", "Phone"), ("int?", "DepartmentId"), ("int?", "LocationId"),
                    ("int?", "ReportingManagerId"), ("string", "ETag")],
        "response": [("int", "Id"), ("string", "FullName"), ("string", "ETag")],
        "responseSummary": "The updated employee.",
        "responseDocs": {
            "Id": "The employee edited.",
            "FullName": "As stored, trimmed.",
            "ETag": "The NEW ConcurrencyStamp. The client must send this one next.",
        },
        "rules": [
            "RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(30);",
            "RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.Email).MaximumLength(256).EmailAddress()",
            "    .When(x => !string.IsNullOrWhiteSpace(x.Email));",
            "RuleFor(x => x.Phone).MaximumLength(40);",
            "RuleFor(x => x.ETag).NotEmpty();",
        ],
        "mapArgs": ["employeeId", "request.EmployeeCode.Trim().ToUpperInvariant()",
                    "request.FullName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim()",
                    "string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()",
                    "request.DepartmentId", "request.LocationId", "request.ReportingManagerId",
                    "request.ETag"],
        "mapCall": "request, employeeId",
        "mapExtra": [("int", "employeeId")],
        "bind": "                int employeeId,\n                UpdateEmployeeRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict", "Status412PreconditionFailed"],
    },
    {
        "name": "DeactivateEmployee", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Mark a leaver inactive. Catalogue: Deactivate a leaver.",
        "capability": MANAGE,
        "verb": "Post", "route": "/employees/{employeeId:int}/deactivate",
        "command": [("int", "EmployeeId"), ("string", "ETag")],
        "request": [("string", "ETag")],
        "response": [("int", "Id"), ("bool", "IsActive"), ("int", "DirectReportsReassigned")],
        "responseSummary": "The deactivated employee.",
        "responseDocs": {
            "Id": "The leaver.",
            "IsActive": "False. The row stays: assets, tickets and history point at it.",
            "DirectReportsReassigned": "How many people reported to this employee and now "
                                       "report to nobody. The caller must give them a new "
                                       "manager; leaving them pointing at a leaver is worse.",
        },
        "rules": ["RuleFor(x => x.ETag).NotEmpty();"],
        "mapArgs": ["employeeId", "request.ETag"],
        "mapCall": "request, employeeId",
        "mapExtra": [("int", "employeeId")],
        "bind": "                int employeeId,\n                DeactivateEmployeeRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status412PreconditionFailed"],
    },
]

if __name__ == "__main__":
    main(SPECS)
