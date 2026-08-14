using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.SetUserCapabilityOverride;

/// <summary>
/// Grant or deny one capability to one person. Catalogue: "Per-user override;
/// a deny wins, so one permission can be withdrawn without unpicking roles."
/// </summary>
/// <remarks>
/// The override is an upsert on the composite key
/// <c>(UserId, CapabilityName)</c>: setting the same capability twice moves
/// the flag rather than failing on the primary key.
/// </remarks>
public sealed class SetUserCapabilityOverrideHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<SetUserCapabilityOverrideCommand, SetUserCapabilityOverrideResponse>
{
    public async Task<Result<SetUserCapabilityOverrideResponse>> HandleAsync(
        SetUserCapabilityOverrideCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userExists = await db.Users.AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            return Error.NotFound("User", request.UserId);
        }

        var capabilityExists = await db.Capabilities.AnyAsync(c => c.Name == request.CapabilityName, ct);
        if (!capabilityExists)
        {
            return Error.NotFound("Capability", request.CapabilityName);
        }

        var existing = await db.UserCapabilityOverrides.SingleOrDefaultAsync(
            o => o.UserId == request.UserId && o.CapabilityName == request.CapabilityName, ct);

        if (existing is null)
        {
            db.UserCapabilityOverrides.Add(new UserCapabilityOverride
            {
                UserId = request.UserId,
                CapabilityName = request.CapabilityName,
                IsGranted = request.IsGranted,
                Reason = request.Reason,
                GrantedOnUtc = clock.UtcNow,
                GrantedBy = currentUser.Username,
            });
        }
        else
        {
            existing.IsGranted = request.IsGranted;
            existing.Reason = request.Reason;
            existing.GrantedOnUtc = clock.UtcNow;
            existing.GrantedBy = currentUser.Username;
        }

        await db.SaveChangesAsync(ct);

        return new SetUserCapabilityOverrideResponse(
            request.UserId, request.CapabilityName, request.IsGranted);
    }
}
