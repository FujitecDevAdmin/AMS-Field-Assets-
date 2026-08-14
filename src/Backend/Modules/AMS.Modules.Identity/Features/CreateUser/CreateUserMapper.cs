using AMS.Modules.Identity.Domain;

namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// Request to command, domain to response. Explicit, greppable,
/// compile-checked — no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateUserMapper
{
    public static CreateUserCommand ToCommand(CreateUserRequest request, string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateUserCommand(
            request.Username.Trim(),
            request.DisplayName.Trim(),
            passwordHash,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            request.EmployeeId,
            request.HasAllBranches,
            request.BranchIds ?? [],
            request.PrimaryBranchId);
    }

    public static CreateUserResponse ToResponse(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new CreateUserResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.MustChangePassword,
            Convert.ToBase64String(user.RowVersion));
    }
}
