namespace AMS.Modules.Identity.Features.UnlockUser;

/// <summary>
/// The account's lock state after the change.
/// </summary>
/// <param name="Id">The account unlocked.</param>
/// <param name="IsLocked">False.</param>
/// <param name="FailedLoginAttempts">Zero. Unlocking without clearing the count would re-lock the account on the next single mistake.</param>
public sealed record UnlockUserResponse(
    int Id,
    bool IsLocked,
    int FailedLoginAttempts);
