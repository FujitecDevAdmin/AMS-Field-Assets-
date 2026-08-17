namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public static class CreateAuditorAccountMapper
{
    public static CreateAuditorAccountCommand ToCommand(
        CreateAuditorAccountRequest request,
        string passwordHash)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CreateAuditorAccountCommand(
            request.Username.Trim(),
            request.DisplayName.Trim(),
            passwordHash,
            string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            request.EmployeeId,
            request.HasAllBranches,
            request.BranchIds ?? [],
            request.PrimaryBranchId,
            request.RequireMfa);
    }
}
