namespace AMS.Modules.Identity.Features.GetUser;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetUserMapper
{
    public static GetUserQuery ToQuery(GetUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetUserQuery(
            request.UserId);
    }
}
