namespace AMS.Modules.Identity.Features.LockUser;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class LockUserMapper
{
    public static LockUserCommand ToCommand(LockUserRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new LockUserCommand(
            userId,
            request.Reason?.Trim());
    }
}
