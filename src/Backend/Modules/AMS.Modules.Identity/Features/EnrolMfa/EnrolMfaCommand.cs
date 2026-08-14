using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.EnrolMfa;

/// <summary>
/// Begin MFA enrolment: issue a secret to scan. Catalogue: Multi-factor authentication.
/// </summary>
public sealed record EnrolMfaCommand(
    int UserId) : ICommand<EnrolMfaResponse>;
