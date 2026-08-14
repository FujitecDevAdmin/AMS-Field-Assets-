namespace AMS.Modules.Identity.Features.LockUser;

/// <summary>
/// The account's lock state after the change.
/// </summary>
/// <param name="Id">The account locked.</param>
/// <param name="IsLocked">True.</param>
public sealed record LockUserResponse(
    int Id,
    bool IsLocked);
