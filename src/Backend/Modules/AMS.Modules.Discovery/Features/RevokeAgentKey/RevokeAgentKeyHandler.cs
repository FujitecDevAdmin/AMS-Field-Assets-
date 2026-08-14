using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.RevokeAgentKey;

/// <summary>Stop a key working. Catalogue: Agent Keys.</summary>
/// <remarks>
/// The row stays. <c>LastUsedOnUtc</c> on a revoked key is how somebody answers
/// "was this key ever used, and until when" after a laptop went missing, and a
/// deleted row answers nothing.
/// </remarks>
public sealed class RevokeAgentKeyHandler(
    DiscoveryDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<RevokeAgentKeyCommand, RevokeAgentKeyResponse>
{
    public async Task<Result<RevokeAgentKeyResponse>> HandleAsync(
        RevokeAgentKeyCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = await db.AgentApiKeys.SingleOrDefaultAsync(k => k.Id == request.Id, ct);
        if (key is null)
        {
            return Error.NotFound("AgentApiKey", request.Id);
        }

        if (!key.IsActive)
        {
            return Error.Conflict(
                "AgentApiKey.AlreadyRevoked",
                $"{key.KeyName} was revoked on {key.RevokedOnUtc:yyyy-MM-dd}.");
        }

        var now = clock.UtcNow;

        key.IsActive = false;
        key.RevokedOnUtc = now;
        key.ModifiedOnUtc = now;
        key.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);

        return new RevokeAgentKeyResponse(key.Id, key.KeyName, now);
    }
}
