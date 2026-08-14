namespace AMS.Modules.Identity.Features.UnlockUser;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UnlockUserMapper
{
    public static UnlockUserCommand ToCommand(UnlockUserRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UnlockUserCommand(
            userId);
    }
}
