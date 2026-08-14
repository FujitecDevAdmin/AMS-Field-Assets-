namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// What the caller gets back. No hash, no MFA secret, nothing the client has
/// no business holding.
/// </summary>
/// <param name="Id">The new user's id.</param>
/// <param name="Username">As stored, trimmed.</param>
/// <param name="DisplayName">As stored, trimmed.</param>
/// <param name="MustChangePassword">Always true for a new account.</param>
/// <param name="ETag">
/// The RowVersion as an opaque base64 string. The client sends it back on the
/// next edit; a mismatch is a 412 (docs/03 §4).
/// </param>
public sealed record CreateUserResponse(
    int Id,
    string Username,
    string DisplayName,
    bool MustChangePassword,
    string ETag);
