using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.CreateSupportTeam;

/// <summary>
/// Add a support team. Catalogue: teams with members, so work can go to a queue
/// rather than a person.
/// </summary>
/// <remarks>
/// Making this team the default does not clear the old one first.
/// UX_SupportTeam_OneDefault is a filtered unique index, so a second default
/// collides on 2601 and returns a 409 naming the problem — which is better than
/// silently demoting a team somebody else chose (03 rule 6).
/// </remarks>
public sealed class CreateSupportTeamHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateSupportTeamCommand, CreateSupportTeamResponse>
{
    public async Task<Result<CreateSupportTeamResponse>> HandleAsync(
        CreateSupportTeamCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var team = new SupportTeam
        {
            TeamName = request.TeamName,
            RegionId = request.RegionId,
            MailboxAddress = request.MailboxAddress,
            IsDefaultTeam = request.IsDefaultTeam,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.SupportTeams.Add(team);

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

        return new CreateSupportTeamResponse(team.Id, team.TeamName, team.IsDefaultTeam);
    }
}
