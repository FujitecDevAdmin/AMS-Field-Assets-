"""
Slice specifications for the Organization module's Applications & Access screen.

Catalogue features 6, 7 and 8: Application master, Grant and revoke application
access, See my application access.

    python build/organization_slices_applications.py
"""
from slices import main

NS = "AMS.Modules.Organization"
PROJECT = "AMS.Modules.Organization"
MANAGE = "Capabilities.Organization.Manage"
VIEW = "Capabilities.Organization.View"
ACCESS = "Capabilities.Organization.ApplicationAccessManage"
EMPLOYEE_VIEW = "Capabilities.Organization.EmployeeView"

SPECS = [
    {
        "name": "SearchApplications", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The application master. Catalogue: the list of business applications "
                   "access can be granted to.",
        "capability": VIEW,
        "verb": "Get", "route": "/applications",
        "command": [("bool?", "IsActive"), ("string?", "Search")],
        "request": [("bool?", "IsActive"), ("string?", "Search")],
        "response": [("IReadOnlyList<SearchApplicationsResponse.Row>", "Rows")],
        "responseSummary": "Every application matching the filter.",
        "responseDocs": {"Rows": "The applications."},
        "rules": ["RuleFor(x => x.Search).MaximumLength(100);"],
        "mapArgs": ["request.IsActive", "request.Search?.Trim()"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchApplicationsRequest request,\n",
    },
    {
        "name": "CreateApplication", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Add a business application. Catalogue: Application master.",
        "capability": MANAGE,
        "verb": "Post", "route": "/applications",
        "command": [("string", "ApplicationName")],
        "request": [("string", "ApplicationName")],
        "response": [("int", "Id"), ("string", "ApplicationName")],
        "responseSummary": "The new application.",
        "responseDocs": {"Id": "The new application.", "ApplicationName": "As stored, trimmed."},
        "rules": ["RuleFor(x => x.ApplicationName).NotEmpty().MaximumLength(100);"],
        "mapArgs": ["request.ApplicationName.Trim()"],
        "mapCall": "request",
        "bind": "                CreateApplicationRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => $\"/api/v1/organization/applications/{response.Id}\")",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "UpdateApplication", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Rename an application or retire it. Catalogue: Application master.",
        "capability": MANAGE,
        "verb": "Put", "route": "/applications/{id:int}",
        "command": [("int", "Id"), ("string", "ApplicationName"), ("bool", "IsActive")],
        "request": [("string", "ApplicationName"), ("bool", "IsActive")],
        "response": [("int", "Id"), ("string", "ApplicationName"), ("bool", "IsActive")],
        "responseSummary": "The updated application.",
        "responseDocs": {
            "Id": "The application edited.",
            "ApplicationName": "As stored, trimmed.",
            "IsActive": "Retiring is deactivation: existing grants still point at it.",
        },
        "rules": ["RuleFor(x => x.ApplicationName).NotEmpty().MaximumLength(100);"],
        "mapArgs": ["id", "request.ApplicationName.Trim()", "request.IsActive"],
        "mapCall": "request, id",
        "mapExtra": [("int", "id")],
        "bind": "                int id,\n                UpdateApplicationRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "GrantApplicationAccess", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Record that an employee may use an application. Catalogue: Grant and "
                   "revoke application access.",
        "capability": ACCESS,
        "verb": "Post", "route": "/employees/{employeeId:int}/applications",
        "command": [("int", "EmployeeId"), ("int", "ApplicationId"), ("string?", "ApplicationLoginId")],
        "request": [("int", "ApplicationId"), ("string?", "ApplicationLoginId")],
        "response": [("int", "Id"), ("int", "EmployeeId"), ("int", "ApplicationId"),
                     ("DateTime", "GrantedOnUtc")],
        "responseSummary": "The grant.",
        "responseDocs": {
            "Id": "The grant row.",
            "EmployeeId": "Who may now use it.",
            "ApplicationId": "What they may use.",
            "GrantedOnUtc": "When. UTC, like every instant.",
        },
        "rules": [
            "RuleFor(x => x.ApplicationId).GreaterThan(0);",
            "RuleFor(x => x.ApplicationLoginId).MaximumLength(100);",
        ],
        "mapArgs": ["employeeId", "request.ApplicationId",
                    "string.IsNullOrWhiteSpace(request.ApplicationLoginId) "
                    "? null : request.ApplicationLoginId.Trim()"],
        "mapCall": "request, employeeId",
        "mapExtra": [("int", "employeeId")],
        "bind": "                int employeeId,\n                GrantApplicationAccessRequest request,\n",
        "successStatus": "Status201Created",
        "result": "ToCreatedResult(response => "
                  "$\"/api/v1/organization/employees/{response.EmployeeId}/applications\")",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "RevokeApplicationAccess", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Withdraw an employee's access to an application. Catalogue: Grant and "
                   "revoke application access.",
        "capability": ACCESS,
        "verb": "Post", "route": "/employees/{employeeId:int}/applications/{applicationId:int}/revoke",
        "command": [("int", "EmployeeId"), ("int", "ApplicationId")],
        "request": [],
        "response": [("int", "Id"), ("DateTime", "RevokedOnUtc")],
        "responseSummary": "The revoked grant.",
        "responseDocs": {
            "Id": "The grant row, which stays: it is the record that access WAS held.",
            "RevokedOnUtc": "When it was withdrawn. UTC.",
        },
        "rules": [],
        "mapArgs": ["employeeId", "applicationId"],
        "mapCall": "new RevokeApplicationAccessRequest(), employeeId, applicationId",
        "mapExtra": [("int", "employeeId"), ("int", "applicationId")],
        "bind": "                int employeeId,\n                int applicationId,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "GetEmployeeApplications", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What one employee has been granted. Catalogue screen: "
                   "Applications and Access.",
        "capability": EMPLOYEE_VIEW,
        "verb": "Get", "route": "/employees/{employeeId:int}/applications",
        "command": [("int", "EmployeeId"), ("bool", "IncludeRevoked")],
        "request": [("int", "EmployeeId"), ("bool?", "IncludeRevoked")],
        "response": [("int", "EmployeeId"), ("IReadOnlyList<GetEmployeeApplicationsResponse.Row>", "Rows")],
        "responseSummary": "One employee's application access.",
        "responseDocs": {
            "EmployeeId": "The employee asked about.",
            "Rows": "Their grants, current and optionally withdrawn.",
        },
        "rules": ["RuleFor(x => x.EmployeeId).GreaterThan(0);"],
        "mapArgs": ["request.EmployeeId", "request.IncludeRevoked ?? false"],
        "mapCall": "new GetEmployeeApplicationsRequest(employeeId, includeRevoked)",
        "bind": "                int employeeId,\n                bool? includeRevoked,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "GetMyApplicationAccess", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "What the signed-in employee has been granted. Catalogue: See my "
                   "application access - a read-only view.",
        "verb": "Get", "route": "/me/applications",
        "command": [("int?", "EmployeeId")],
        "request": [],
        "response": [("int?", "EmployeeId"),
                     ("IReadOnlyList<GetMyApplicationAccessResponse.Row>", "Rows")],
        "responseSummary": "The caller's own application access.",
        "responseDocs": {
            "EmployeeId": "Null when this login has no employee record - a service account, "
                          "or an administrator who is not in the directory.",
            "Rows": "Current grants only. An employee has no reason to see what was "
                    "withdrawn from them.",
        },
        "rules": [],
        "mapArgs": ["currentUser.EmployeeId"],
        "mapCall": "new GetMyApplicationAccessRequest(), currentUser",
        "mapExtra": [("AMS.SharedKernel.Abstractions.ICurrentUser", "currentUser")],
        "bind": "                ICurrentUser currentUser,\n",
    },
]

if __name__ == "__main__":
    main(SPECS)
