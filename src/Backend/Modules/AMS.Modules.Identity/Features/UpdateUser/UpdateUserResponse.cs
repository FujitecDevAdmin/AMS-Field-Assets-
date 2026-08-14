namespace AMS.Modules.Identity.Features.UpdateUser;

/// <summary>
/// The updated user.
/// </summary>
/// <param name="Id">The user edited.</param>
/// <param name="DisplayName">As stored, trimmed.</param>
/// <param name="ETag">The NEW RowVersion. The client must send this one on the next edit.</param>
public sealed record UpdateUserResponse(
    int Id,
    string DisplayName,
    string ETag);
