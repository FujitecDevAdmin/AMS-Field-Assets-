namespace AMS.Modules.Identity.Features.UpdateUser;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateUserMapper
{
    public static UpdateUserCommand ToCommand(UpdateUserRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateUserCommand(
            userId,
            request.DisplayName.Trim(),
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            request.EmployeeId,
            request.IsActive,
            request.HasAllBranches,
            request.ETag);
    }
}
