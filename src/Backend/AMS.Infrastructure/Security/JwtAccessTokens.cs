using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AMS.SharedKernel.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AMS.Infrastructure.Security;

/// <summary>
/// Issues signed JWTs carrying the caller's identity, branch scope and
/// capabilities.
/// </summary>
/// <remarks>
/// <para>
/// The capabilities travel IN the token. That is what keeps
/// <c>[Identity]</c> out of the path of every request in every other module —
/// rule 2 — at the cost that a capability change reaches somebody when their
/// next token is issued rather than instantly.
/// </para>
/// <para>
/// A token for a user with many capabilities is a large header. That is
/// accepted: the alternative is a database round trip per request, per module,
/// forever.
/// </para>
/// </remarks>
public sealed class JwtAccessTokens(IOptions<JwtOptions> options, IClock clock) : IAccessTokens
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Issue(AccessTokenSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var expires = clock.UtcNow.AddMinutes(_options.LifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.UserId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, subject.Username),
            new(AmsClaims.UserId, subject.UserId.ToString(CultureInfo.InvariantCulture)),
            new(AmsClaims.AllBranches, subject.HasAllBranches ? "true" : "false"),
        };

        if (subject.EmployeeId is { } employeeId)
        {
            claims.Add(new Claim(AmsClaims.EmployeeId, employeeId.ToString(CultureInfo.InvariantCulture)));
        }

        // Branch claims are omitted entirely for head office. An empty list and
        // "sees everything" are different things, and conflating them is how a
        // scoping bug becomes a data leak.
        if (!subject.HasAllBranches)
        {
            claims.AddRange(subject.BranchIds.Select(
                id => new Claim(AmsClaims.Branch, id.ToString(CultureInfo.InvariantCulture))));
        }

        claims.AddRange(subject.Capabilities.Select(c => new Claim(AmsClaims.Capability, c)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: clock.UtcNow,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
