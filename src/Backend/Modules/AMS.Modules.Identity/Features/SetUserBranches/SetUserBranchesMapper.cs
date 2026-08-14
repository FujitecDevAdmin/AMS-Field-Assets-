namespace AMS.Modules.Identity.Features.SetUserBranches;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetUserBranchesMapper
{
    public static SetUserBranchesCommand ToCommand(SetUserBranchesRequest request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetUserBranchesCommand(
            userId,
            request.BranchIds,
            request.PrimaryBranchId);
    }
}
