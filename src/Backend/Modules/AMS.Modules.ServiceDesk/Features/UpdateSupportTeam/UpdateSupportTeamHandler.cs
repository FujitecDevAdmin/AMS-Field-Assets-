using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.UpdateSupportTeam;

/// <summary>Edit a team or retire it.</summary>
/// <remarks>
/// The default team cannot be retired. It is where routing sends anything it
/// cannot place, so an inactive default means tickets with nowhere to go and
/// nobody watching them.
/// </remarks>
public sealed class UpdateSupportTeamHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateSupportTeamCommand, UpdateSupportTeamResponse>
{
    public async Task<Result<UpdateSupportTeamResponse>> HandleAsync(
        UpdateSupportTeamCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var team = await db.SupportTeams.SingleOrDefaultAsync(t => t.Id == request.Id, ct);
        if (team is null)
        {
            return Error.NotFound("SupportTeam", request.Id);
        }

        if (request.IsDefaultTeam && !request.IsActive)
        {
            return Error.Validation(
                "SupportTeam.DefaultMustStayActive",
                "The default team cannot be retired: it is where unroutable tickets go.");
        }

        team.TeamName = request.TeamName;
        team.RegionId = request.RegionId;
        team.MailboxAddress = request.MailboxAddress;
        team.IsDefaultTeam = request.IsDefaultTeam;
        team.IsActive = request.IsActive;
        team.ModifiedOnUtc = clock.UtcNow;
        team.ModifiedBy = currentUser.Username;

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

        return new UpdateSupportTeamResponse(team.Id, team.TeamName, team.IsActive);
    }
}
