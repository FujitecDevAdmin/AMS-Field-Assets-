namespace AMS.Modules.Identity.Features.ChangeMyPassword;

/// <summary>
/// The result of a password change.
/// </summary>
/// <param name="UserId">Themselves.</param>
/// <param name="MustChangePassword">Always false afterwards - that is the point.</param>
public sealed record ChangeMyPasswordResponse(
    int UserId,
    bool MustChangePassword);
