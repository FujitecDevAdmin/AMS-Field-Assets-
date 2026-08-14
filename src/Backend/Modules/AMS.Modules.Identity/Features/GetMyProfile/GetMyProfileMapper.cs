namespace AMS.Modules.Identity.Features.GetMyProfile;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetMyProfileMapper
{
    public static GetMyProfileQuery ToQuery(GetMyProfileRequest request, AMS.SharedKernel.Abstractions.ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetMyProfileQuery(
            currentUser.Id);
    }
}
