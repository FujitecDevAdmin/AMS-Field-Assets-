namespace AMS.Modules.Identity.Features.ListAuditorAccounts;

public sealed record ListAuditorAccountsResponse(IReadOnlyList<ListAuditorAccountsResponse.Row> Rows)
{
    public sealed record Row(int Id, string Username, string DisplayName, string? Email,
        int? EmployeeId, bool HasAllBranches, IReadOnlyList<int> BranchIds, bool IsActive,
        bool IsLocked, bool MfaEnabled, DateTime? LastLoginOnUtc);
}
