using Microsoft.AspNetCore.Builder;

namespace AMS.SharedKernel.Web.Http;

/// <summary>
/// Declares the capability an endpoint requires.
/// </summary>
/// <remarks>
/// Authorisation is by capability, never by role name (docs/01 §2 rule 6). The
/// name is resolved against the caller's effective set — the union of their
/// roles' grants, minus any per-user deny, because a deny must win.
/// </remarks>
public static class CapabilityExtensions
{
    /// <summary>The policy prefix under which capability policies are registered.</summary>
    public const string PolicyPrefix = "capability:";

    public static TBuilder RequireCapability<TBuilder>(this TBuilder builder, string capability)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(capability);

        builder.RequireAuthorization(PolicyPrefix + capability);
        return builder;
    }
}
