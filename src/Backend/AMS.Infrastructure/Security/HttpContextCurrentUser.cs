using System.Globalization;
using System.Security.Claims;
using AMS.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Http;

namespace AMS.Infrastructure.Security;

/// <summary>
/// Who is asking, read from the bearer token on the current request.
/// </summary>
/// <remarks>
/// <para>
/// Everything here comes off claims resolved once at sign-in, not from a query
/// per request. A capability check that hit the database would put
/// <c>[Identity]</c> in the path of every request in every module, which is the
/// coupling rule 2 exists to prevent.
/// </para>
/// <para>
/// The consequence is deliberate and worth knowing: changing somebody's roles
/// takes effect when their token is next issued, not instantly. Locking an
/// account is the case that cannot wait, and lockout is checked at sign-in, so
/// a locked user cannot get a new token.
/// </para>
/// </remarks>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int Id => Int(AmsClaims.UserId) ?? 0;

    public string Username => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public int? EmployeeId => Int(AmsClaims.EmployeeId);

    public bool HasAllBranches =>
        string.Equals(
            Principal?.FindFirstValue(AmsClaims.AllBranches), "true", StringComparison.Ordinal);

    public IReadOnlySet<int> BranchIds =>
        Principal?.FindAll(AmsClaims.Branch)
            .Select(c => int.TryParse(c.Value, CultureInfo.InvariantCulture, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet()
        ?? [];

    public IReadOnlySet<string> Capabilities =>
        Principal?.FindAll(AmsClaims.Capability)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal)
        ?? new HashSet<string>(StringComparer.Ordinal);

    private int? Int(string claimType) =>
        int.TryParse(
            Principal?.FindFirstValue(claimType), CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
