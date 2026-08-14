using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.GetUserCapabilities;

/// <summary>
/// The effective capability set for one user. A query: it never mutates and
/// never calls SaveChanges (docs/01 §3).
/// </summary>
public sealed record GetUserCapabilitiesQuery(int UserId) : IQuery<GetUserCapabilitiesResponse>;
