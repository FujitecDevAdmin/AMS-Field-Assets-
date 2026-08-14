"""
Slice specifications for the Identity module's Users screen.

Catalogue features: Create and maintain users, Assign roles, Set which
branches a user sees. CreateUser already exists and is not respecified here.

    python build/identity_slices_users.py
"""
from slices import main

NS = "AMS.Modules.Identity"
PROJECT = "AMS.Modules.Identity"
MANAGE = "Capabilities.Identity.UserManage"
VIEW = "Capabilities.Identity.UserView"

SPECS = [
    {
        "name": "SearchUsers", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The Users list, filtered and paged.",
        "capability": VIEW,
        "verb": "Get", "route": "/users",
        "command": [("string?", "Search"), ("bool?", "IsActive"), ("int", "Skip"), ("int", "Take")],
        "request": [("string?", "Search"), ("bool?", "IsActive"), ("int?", "Skip"), ("int?", "Take")],
        "response": [("IReadOnlyList<SearchUsersRow>", "Rows"), ("int", "TotalCount")],
        "responseSummary": "One page of users, and how many match in total.",
        "responseDocs": {
            "Rows": "The page.",
            "TotalCount": "Rows matching the filter, ignoring paging. The grid needs it to "
                          "size the scrollbar (docs/04 §3).",
        },
        "rules": [
            "RuleFor(x => x.Search).MaximumLength(100);",
            "RuleFor(x => x.Skip).GreaterThanOrEqualTo(0).When(x => x.Skip.HasValue);",
            "// An unbounded list on a business table is a review-blocker (02 §8).",
            "RuleFor(x => x.Take).InclusiveBetween(1, 200).When(x => x.Take.HasValue);",
        ],
        "mapArgs": ["request.Search?.Trim()", "request.IsActive", "request.Skip ?? 0", "request.Take ?? 50"],
        "mapCall": "request",
        "bind": "                [AsParameters] SearchUsersRequest request,\n",
    },
    {
        "name": "GetUser", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "One user, with the roles and branches the screen edits.",
        "capability": VIEW,
        "verb": "Get", "route": "/users/{userId:int}",
        "command": [("int", "UserId")],
        "request": [("int", "UserId")],
        "response": [("int", "Id"), ("string", "Username"), ("string", "DisplayName"),
                     ("string?", "Email"), ("int?", "EmployeeId"), ("bool", "IsActive"),
                     ("bool", "IsLocked"), ("bool", "MustChangePassword"), ("bool", "MfaEnabled"),
                     ("bool", "HasAllBranches"), ("IReadOnlyList<int>", "RoleIds"),
                     ("IReadOnlyList<int>", "BranchIds"), ("int?", "PrimaryBranchId"),
                     ("string", "ETag")],
        "responseSummary": "Everything the Users screen shows for one person.",
        "responseDocs": {
            "ETag": "RowVersion, base64. Carried back on the next edit; a mismatch is a 412.",
            "RoleIds": "Roles held, whether or not the role itself is active.",
            "BranchIds": "Empty when HasAllBranches is true.",
            "PrimaryBranchId": "At most one, enforced by UX_UserBranch_OnePrimary.",
        },
        "rules": ["RuleFor(x => x.UserId).GreaterThan(0);"],
        "mapArgs": ["request.UserId"],
        "mapCall": "new GetUserRequest(userId)",
        "bind": "                int userId,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "UpdateUser", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Edit a user. Catalogue: Create and maintain users.",
        "capability": MANAGE,
        "verb": "Put", "route": "/users/{userId:int}",
        "command": [("int", "UserId"), ("string", "DisplayName"), ("string?", "Email"),
                    ("int?", "EmployeeId"), ("bool", "IsActive"), ("bool", "HasAllBranches"),
                    ("string", "ETag")],
        "request": [("string", "DisplayName"), ("string?", "Email"), ("int?", "EmployeeId"),
                    ("bool", "IsActive"), ("bool", "HasAllBranches"), ("string", "ETag")],
        "response": [("int", "Id"), ("string", "DisplayName"), ("string", "ETag")],
        "responseSummary": "The updated user.",
        "responseDocs": {
            "Id": "The user edited.",
            "DisplayName": "As stored, trimmed.",
            "ETag": "The NEW RowVersion. The client must send this one on the next edit.",
        },
        "rules": [
            "RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(150);",
            "RuleFor(x => x.Email).MaximumLength(256).EmailAddress()",
            "    .When(x => !string.IsNullOrWhiteSpace(x.Email));",
            "RuleFor(x => x.ETag).NotEmpty();",
        ],
        "mapArgs": ["userId", "request.DisplayName.Trim()",
                    "string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim()",
                    "request.EmployeeId", "request.IsActive", "request.HasAllBranches", "request.ETag"],
        "mapCall": "request, userId",
        "mapExtra": [("int", "userId")],
        "bind": "                int userId,\n                UpdateUserRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict", "Status412PreconditionFailed"],
    },
    {
        "name": "LockUser", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Lock an account. Catalogue: Create and maintain users.",
        "capability": MANAGE,
        "verb": "Post", "route": "/users/{userId:int}/lock",
        "command": [("int", "UserId"), ("string?", "Reason")],
        "request": [("string?", "Reason")],
        "response": [("int", "Id"), ("bool", "IsLocked")],
        "responseSummary": "The account's lock state after the change.",
        "responseDocs": {"Id": "The account locked.", "IsLocked": "True."},
        "rules": ["RuleFor(x => x.Reason).MaximumLength(300);"],
        "mapArgs": ["userId", "request.Reason?.Trim()"],
        "mapCall": "request, userId",
        "mapExtra": [("int", "userId")],
        "bind": "                int userId,\n                LockUserRequest request,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "UnlockUser", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Unlock an account and clear its failure count. Catalogue: "
                   "Create and maintain users, Account lockout.",
        "capability": MANAGE,
        "verb": "Post", "route": "/users/{userId:int}/unlock",
        "command": [("int", "UserId")],
        "request": [],
        "response": [("int", "Id"), ("bool", "IsLocked"), ("int", "FailedLoginAttempts")],
        "responseSummary": "The account's lock state after the change.",
        "responseDocs": {
            "Id": "The account unlocked.",
            "IsLocked": "False.",
            "FailedLoginAttempts": "Zero. Unlocking without clearing the count would re-lock "
                                   "the account on the next single mistake.",
        },
        "rules": [],
        "mapArgs": ["userId"],
        "mapCall": "new UnlockUserRequest(), userId",
        "mapExtra": [("int", "userId")],
        "bind": "                int userId,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "ResetUserPassword", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Set a temporary password the user must then change. Catalogue: "
                   "Create and maintain users, Forced password change.",
        "capability": MANAGE,
        "verb": "Post", "route": "/users/{userId:int}/password-reset",
        "command": [("int", "UserId"), ("string", "NewPassword")],
        "request": [("string", "NewPassword")],
        "response": [("int", "Id"), ("bool", "MustChangePassword")],
        "responseSummary": "The reset account.",
        "responseDocs": {
            "Id": "The account reset.",
            "MustChangePassword": "Always true: an administrator has just seen this password.",
        },
        "rules": ["RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(256);"],
        "mapArgs": ["userId", "request.NewPassword"],
        "mapCall": "request, userId",
        "mapExtra": [("int", "userId")],
        "bind": "                int userId,\n                ResetUserPasswordRequest request,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "AssignUserRoles", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Replace the roles a user holds. Catalogue: Assign roles.",
        "capability": MANAGE,
        "verb": "Put", "route": "/users/{userId:int}/roles",
        "command": [("int", "UserId"), ("IReadOnlyList<int>", "RoleIds")],
        "request": [("IReadOnlyList<int>", "RoleIds")],
        "response": [("int", "UserId"), ("IReadOnlyList<int>", "RoleIds")],
        "responseSummary": "The roles the user now holds.",
        "responseDocs": {
            "UserId": "The user changed.",
            "RoleIds": "The complete set afterwards, not a delta.",
        },
        "rules": [
            "RuleFor(x => x.RoleIds).NotNull();",
            "RuleForEach(x => x.RoleIds).GreaterThan(0);",
        ],
        "mapArgs": ["userId", "request.RoleIds"],
        "mapCall": "request, userId",
        "mapExtra": [("int", "userId")],
        "bind": "                int userId,\n                AssignUserRolesRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
    {
        "name": "SetUserBranches", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Replace the branches a user sees. Catalogue: Set which branches a user sees.",
        "capability": MANAGE,
        "verb": "Put", "route": "/users/{userId:int}/branches",
        "command": [("int", "UserId"), ("IReadOnlyList<int>", "BranchIds"), ("int?", "PrimaryBranchId")],
        "request": [("IReadOnlyList<int>", "BranchIds"), ("int?", "PrimaryBranchId")],
        "response": [("int", "UserId"), ("IReadOnlyList<int>", "BranchIds"), ("int?", "PrimaryBranchId")],
        "responseSummary": "The branches the user now sees.",
        "responseDocs": {
            "UserId": "The user changed.",
            "BranchIds": "The complete set afterwards.",
            "PrimaryBranchId": "At most one; UX_UserBranch_OnePrimary is what enforces that.",
        },
        "rules": [
            "RuleFor(x => x.BranchIds).NotNull();",
            "RuleForEach(x => x.BranchIds).GreaterThan(0);",
            "RuleFor(x => x.PrimaryBranchId)",
            "    .Must((request, primary) => primary is null || request.BranchIds.Contains(primary.Value))",
            "    .WithMessage(\"The primary branch must be one of the branches granted.\");",
        ],
        "mapArgs": ["userId", "request.BranchIds", "request.PrimaryBranchId"],
        "mapCall": "request, userId",
        "mapExtra": [("int", "userId")],
        "bind": "                int userId,\n                SetUserBranchesRequest request,\n",
        "otherStatuses": ["Status404NotFound", "Status409Conflict"],
    },
]

if __name__ == "__main__":
    main(SPECS)
