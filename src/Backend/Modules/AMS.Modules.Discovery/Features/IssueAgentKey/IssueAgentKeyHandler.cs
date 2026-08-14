using AMS.Modules.Discovery.Agents;
using AMS.Modules.Discovery.Domain;
using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Features.IssueAgentKey;

/// <summary>Mint a key for an agent. Catalogue: Agent Keys.</summary>
/// <remarks>
/// The secret is in the response and nowhere else. The database keeps a hash,
/// which is the point of a hash: an administrator who loses the key issues
/// another one, and nobody with database access can read what the agents are
/// using.
/// </remarks>
public sealed class IssueAgentKeyHandler(
    DiscoveryDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<IssueAgentKeyCommand, IssueAgentKeyResponse>
{
    public async Task<Result<IssueAgentKeyResponse>> HandleAsync(
        IssueAgentKeyCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var issued = AgentKeys.Issue();

        var key = new AgentApiKey
        {
            KeyName = request.KeyName,
            KeyPrefix = issued.Prefix,
            KeyHash = issued.Hash,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.AgentApiKeys.Add(key);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new IssueAgentKeyResponse(key.Id, key.KeyName, issued.Key, key.KeyPrefix);
    }
}
