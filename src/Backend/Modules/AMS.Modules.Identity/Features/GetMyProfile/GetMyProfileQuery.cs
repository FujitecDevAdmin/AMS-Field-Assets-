using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.GetMyProfile;

/// <summary>
/// The signed-in user's own record. Catalogue screen: My Profile.
/// </summary>
public sealed record GetMyProfileQuery(
    int UserId) : IQuery<GetMyProfileResponse>;
