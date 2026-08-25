namespace AMS.Modules.Organization.Features.CreateBranch;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateBranchMapper
{
    public static CreateBranchCommand ToCommand(CreateBranchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateBranchCommand(
            request.BranchCode.Trim().ToUpperInvariant(),
            request.BranchName.Trim(),
            request.RegionId,
            request.Latitude,
            request.Longitude,
            request.TimeZoneId.Trim(),
            request.IsHeadOffice);
    }
}
