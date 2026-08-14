using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.SetSupportTeamMembers;

/// <summary>
/// Set who is in a team and who leads it. Catalogue: Teams, members and leads.
/// </summary>
/// <remarks>
/// The whole membership at once, not one person at a time. A team is a set, and
/// "add" and "remove" endpoints make the screen do arithmetic to work out what
/// changed — arithmetic that goes wrong when two administrators edit at once.
/// </remarks>
public sealed class SetSupportTeamMembersHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetSupportTeamMembersCommand, SetSupportTeamMembersResponse>
{
    public async Task<Result<SetSupportTeamMembersResponse>> HandleAsync(
        SetSupportTeamMembersCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var team = await db.SupportTeams
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == request.SupportTeamId, ct);
        if (team is null)
        {
            return Error.NotFound("SupportTeam", request.SupportTeamId);
        }

        var members = request.Members
            .GroupBy(m => m.UserId)
            // Somebody listed twice, once as lead: the lead wins. Refusing
            // would fail a save over something the screen can mean only one way.
            .Select(g => new { UserId = g.Key, IsLead = g.Any(m => m.IsLead) })
            .ToList();

        if (members.Count > 0 && members.TrueForAll(m => !m.IsLead))
        {
            return Error.Validation(
                "SupportTeam.NoLead",
                "A team with members needs at least one lead — escalation has to reach somebody.");
        }

        var existing = await db.SupportTeamMembers
            .Where(m => m.SupportTeamId == request.SupportTeamId)
            .ToListAsync(ct);

        db.SupportTeamMembers.RemoveRange(
            existing.Where(e => !members.Exists(m => m.UserId == e.UserId)));

        foreach (var member in members)
        {
            var row = existing.Find(e => e.UserId == member.UserId);
            if (row is null)
            {
                db.SupportTeamMembers.Add(new SupportTeamMember
                {
                    SupportTeamId = request.SupportTeamId,
                    UserId = member.UserId,
                    IsLead = member.IsLead,
                    AddedOnUtc = clock.UtcNow,
                    AddedByUserId = currentUser.Id,
                });
            }
            else
            {
                // Kept, not replaced: AddedOnUtc is when they joined, and
                // promoting somebody to lead must not rewrite that.
                row.IsLead = member.IsLead;
            }
        }

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

        return new SetSupportTeamMembersResponse(
            request.SupportTeamId, members.Count, members.Count(m => m.IsLead));
    }
}
