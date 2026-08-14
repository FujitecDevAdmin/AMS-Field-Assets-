using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AMS.Infrastructure.Security;

/// <summary>
/// Builds an authorization policy for every <c>capability:</c> name an endpoint
/// asks for, on demand.
/// </summary>
/// <remarks>
/// <para>
/// <c>RequireCapability("asset.view")</c> registers a policy named
/// <c>capability:asset.view</c>. Something has to produce that policy, and the
/// alternative is a startup list naming all ninety-odd capabilities — a list
/// whose only failure mode is silence: an endpoint asking for a policy nobody
/// registered returns 500 at the moment somebody first opens the screen.
/// </para>
/// <para>
/// Generated instead. An endpoint's declaration IS the registration, so the two
/// cannot drift. Whether the capability is one an administrator can actually
/// grant is a different question, and the seed in Section 17.6 answers it.
/// </para>
/// </remarks>
public sealed class CapabilityPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        // An explicitly registered policy of the same name wins, so a
        // capability that ever needs extra requirements can be declared by hand
        // without changing this class.
        var declared = await base.GetPolicyAsync(policyName);
        if (declared is not null)
        {
            return declared;
        }

        if (!policyName.StartsWith(CapabilityExtensions.PolicyPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var capability = policyName[CapabilityExtensions.PolicyPrefix.Length..];

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(AmsClaims.Capability, capability)
            .Build();
    }
}
