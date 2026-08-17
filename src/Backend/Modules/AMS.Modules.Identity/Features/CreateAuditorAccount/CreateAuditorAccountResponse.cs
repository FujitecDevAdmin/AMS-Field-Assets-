namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public sealed record CreateAuditorAccountResponse(
    int Id,
    string Username,
    string DisplayName,
    bool MustChangePassword,
    bool MfaEnrollmentRequired);
