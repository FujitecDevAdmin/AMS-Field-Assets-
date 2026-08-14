namespace AMS.Modules.Identity.Features.ResetUserPassword;

/// <summary>
/// The reset account.
/// </summary>
/// <param name="Id">The account reset.</param>
/// <param name="MustChangePassword">Always true: an administrator has just seen this password.</param>
public sealed record ResetUserPasswordResponse(
    int Id,
    bool MustChangePassword);
