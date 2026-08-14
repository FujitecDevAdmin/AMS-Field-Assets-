"""
Slice specifications for AMS.Modules.Identity.

Every screen and feature the catalogue lists for the Identity module maps to a
slice here. Run:

    python build/identity_slices.py            # all
    python build/identity_slices.py SignIn     # one

Handlers are never generated - they hold the logic and are written by hand.
"""
from slices import main

NS = "AMS.Modules.Identity"
PROJECT = "AMS.Modules.Identity"
MANAGE = "Capabilities.Identity.UserManage"
VIEW = "Capabilities.Identity.UserView"
ROLES = "Capabilities.Identity.RoleManage"

SPECS = [
    # ---------------------------------------------------------------- Sign In
    {
        "name": "SignIn", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Authenticate a username and password. Catalogue: Sign in, "
                   "Forced password change, Account lockout.",
        "anonymous": True,
        "verb": "Post", "route": "/sign-in",
        "command": [("string", "Username"), ("string", "Password")],
        "request": [("string", "Username"), ("string", "Password")],
        "response": [("int", "UserId"), ("string", "Username"), ("string", "DisplayName"),
                     ("bool", "MustChangePassword"), ("bool", "MfaRequired"),
                     ("string?", "MfaChallengeToken")],
        "responseSummary": "The outcome of a sign-in attempt. Never says WHY it failed.",
        "responseDocs": {
            "UserId": "The signed-in user.",
            "Username": "As stored.",
            "DisplayName": "For the application header.",
            "MustChangePassword": "True for a new or admin-reset account; the client must "
                                  "route to the password change screen before anything else.",
            "MfaRequired": "True when the user is enrolled. The session is NOT usable until "
                           "VerifyMfaCode succeeds.",
            "MfaChallengeToken": "Short-lived token identifying this half-finished sign-in. "
                                 "Null when MFA is not required.",
        },
        "rules": [
            "RuleFor(x => x.Username).NotEmpty().MaximumLength(100);",
            "RuleFor(x => x.Password).NotEmpty().MaximumLength(256);",
        ],
        "mapArgs": ["request.Username.Trim()", "request.Password"],
        "mapCall": "request",
        "bind": "                SignInRequest request,\n",
        "otherStatuses": ["Status401Unauthorized"],
    },
    {
        "name": "VerifyMfaCode", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Complete a sign-in with an authenticator code or a single-use "
                   "recovery code. Catalogue: Multi-factor authentication.",
        "anonymous": True,
        "verb": "Post", "route": "/sign-in/mfa",
        "command": [("string", "MfaChallengeToken"), ("string", "Code")],
        "request": [("string", "MfaChallengeToken"), ("string", "Code")],
        "response": [("int", "UserId"), ("string", "Username"), ("string", "DisplayName"),
                     ("bool", "MustChangePassword"), ("bool", "UsedRecoveryCode"),
                     ("int", "RemainingRecoveryCodes")],
        "responseSummary": "A completed sign-in.",
        "responseDocs": {
            "UserId": "The signed-in user.",
            "Username": "As stored.",
            "DisplayName": "For the application header.",
            "MustChangePassword": "Carried through from the password step.",
            "UsedRecoveryCode": "True when a recovery code was spent rather than an "
                                "authenticator code. The client should say so.",
            "RemainingRecoveryCodes": "How many are left. Prompts regeneration near zero.",
        },
        "rules": [
            "RuleFor(x => x.MfaChallengeToken).NotEmpty().MaximumLength(500);",
            "// Six digits for an authenticator code, longer for a recovery code.",
            "RuleFor(x => x.Code).NotEmpty().MinimumLength(6).MaximumLength(64);",
        ],
        "mapArgs": ["request.MfaChallengeToken", "request.Code.Trim()"],
        "mapCall": "request",
        "bind": "                VerifyMfaCodeRequest request,\n",
        "otherStatuses": ["Status401Unauthorized"],
    },
    # ------------------------------------------------------------- My Profile
    # No capability on these: every signed-in user may read and change their
    # OWN record. A capability would be a lie - withdrawing it would lock
    # somebody out of their own password change.
    {
        "name": "GetMyProfile", "kind": "query", "ns": NS, "project": PROJECT,
        "summary": "The signed-in user's own record. Catalogue screen: My Profile.",
        "verb": "Get", "route": "/me",
        "command": [("int", "UserId")],
        "request": [],
        "response": [("int", "UserId"), ("string", "Username"), ("string", "DisplayName"),
                     ("string?", "Email"), ("bool", "MustChangePassword"),
                     ("bool", "MfaEnabled"), ("int", "RemainingRecoveryCodes"),
                     ("bool", "HasAllBranches"), ("IReadOnlyList<int>", "BranchIds")],
        "responseSummary": "What the signed-in user may see about themselves.",
        "responseDocs": {
            "UserId": "Themselves.",
            "Username": "As stored.",
            "DisplayName": "For the application header.",
            "Email": "May be null; not every user has one.",
            "MustChangePassword": "True until they set their own password.",
            "MfaEnabled": "True once enrolment is confirmed, not merely started.",
            "RemainingRecoveryCodes": "Unused codes. The profile screen nags near zero.",
            "HasAllBranches": "Head office.",
            "BranchIds": "Empty when HasAllBranches is true.",
        },
        "rules": [],
        "mapArgs": ["currentUser.Id"],
        "mapCall": "new GetMyProfileRequest(), currentUser",
        "mapExtra": [("AMS.SharedKernel.Abstractions.ICurrentUser", "currentUser")],
        "bind": "                ICurrentUser currentUser,\n",
        "otherStatuses": ["Status404NotFound"],
    },
    {
        "name": "ChangeMyPassword", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Set my own password. Catalogue: Change my own password, and the "
                   "second half of Forced password change.",
        "verb": "Post", "route": "/me/password",
        "command": [("int", "UserId"), ("string", "CurrentPassword"), ("string", "NewPassword")],
        "request": [("string", "CurrentPassword"), ("string", "NewPassword")],
        "response": [("int", "UserId"), ("bool", "MustChangePassword")],
        "responseSummary": "The result of a password change.",
        "responseDocs": {
            "UserId": "Themselves.",
            "MustChangePassword": "Always false afterwards - that is the point.",
        },
        "rules": [
            "RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(256);",
            "RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(256);",
            "RuleFor(x => x.NewPassword)",
            "    .NotEqual(x => x.CurrentPassword)",
            "    .WithMessage(\"The new password must be different from the current one.\");",
        ],
        "mapArgs": ["currentUser.Id", "request.CurrentPassword", "request.NewPassword"],
        "mapCall": "request, currentUser",
        "mapExtra": [("AMS.SharedKernel.Abstractions.ICurrentUser", "currentUser")],
        "bind": "                ChangeMyPasswordRequest request,\n                ICurrentUser currentUser,\n",
        "otherStatuses": ["Status403Forbidden"],
    },
    {
        "name": "EnrolMfa", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Begin MFA enrolment: issue a secret to scan. Catalogue: "
                   "Multi-factor authentication.",
        "verb": "Post", "route": "/me/mfa/enrol",
        "command": [("int", "UserId")],
        "request": [],
        "response": [("string", "Secret"), ("string", "OtpAuthUri")],
        "responseSummary": "The secret to enrol with. Returned ONCE and never readable again.",
        "responseDocs": {
            "Secret": "Base32, for typing in by hand when a camera will not cooperate.",
            "OtpAuthUri": "otpauth:// URI for the QR code.",
        },
        "rules": [],
        "mapArgs": ["currentUser.Id"],
        "mapCall": "new EnrolMfaRequest(), currentUser",
        "mapExtra": [("AMS.SharedKernel.Abstractions.ICurrentUser", "currentUser")],
        "bind": "                ICurrentUser currentUser,\n",
        "otherStatuses": ["Status409Conflict"],
    },
    {
        "name": "ConfirmMfaEnrolment", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Prove the authenticator works, turn MFA on, and issue recovery "
                   "codes. Catalogue: Multi-factor authentication.",
        "verb": "Post", "route": "/me/mfa/confirm",
        "command": [("int", "UserId"), ("string", "Code")],
        "request": [("string", "Code")],
        "response": [("bool", "MfaEnabled"), ("IReadOnlyList<string>", "RecoveryCodes")],
        "responseSummary": "Enrolment confirmed, with the recovery codes.",
        "responseDocs": {
            "MfaEnabled": "True. Sign-in will challenge from now on.",
            "RecoveryCodes": "Shown ONCE. Only hashes are stored, so nobody - including "
                             "an administrator - can ever read them back.",
        },
        "rules": [
            "RuleFor(x => x.Code).NotEmpty().Length(6);",
        ],
        "mapArgs": ["currentUser.Id", "request.Code.Trim()"],
        "mapCall": "request, currentUser",
        "mapExtra": [("AMS.SharedKernel.Abstractions.ICurrentUser", "currentUser")],
        "bind": "                ConfirmMfaEnrolmentRequest request,\n                ICurrentUser currentUser,\n",
        "otherStatuses": ["Status403Forbidden"],
    },
    {
        "name": "RegenerateRecoveryCodes", "kind": "command", "ns": NS, "project": PROJECT,
        "summary": "Replace every recovery code with a fresh set. Catalogue: "
                   "Multi-factor authentication.",
        "verb": "Post", "route": "/me/mfa/recovery-codes",
        "command": [("int", "UserId"), ("string", "Code")],
        "request": [("string", "Code")],
        "response": [("IReadOnlyList<string>", "RecoveryCodes")],
        "responseSummary": "A fresh set. Every previous code stops working immediately.",
        "responseDocs": {
            "RecoveryCodes": "Shown ONCE, like the originals.",
        },
        "rules": [
            "// Re-prove the second factor first: regenerating from a hijacked "
            "session would lock the real owner out permanently.",
            "RuleFor(x => x.Code).NotEmpty().Length(6);",
        ],
        "mapArgs": ["currentUser.Id", "request.Code.Trim()"],
        "mapCall": "request, currentUser",
        "mapExtra": [("AMS.SharedKernel.Abstractions.ICurrentUser", "currentUser")],
        "bind": "                RegenerateRecoveryCodesRequest request,\n                ICurrentUser currentUser,\n",
        "otherStatuses": ["Status403Forbidden"],
    },
]

if __name__ == "__main__":
    main(SPECS)
