namespace AMS.Modules.Organization.Features.UpdateBranch;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateBranchMapper
{
    public static UpdateBranchCommand ToCommand(UpdateBranchRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateBranchCommand(
            id,
            request.BranchCode.Trim().ToUpperInvariant(),
            request.BranchName.Trim(),
            request.RegionId,
            request.TimeZoneId.Trim(),
            request.IsHeadOffice,
            request.IsActive);
    }
}
