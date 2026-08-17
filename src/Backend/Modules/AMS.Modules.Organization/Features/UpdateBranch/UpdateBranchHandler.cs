using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.UpdateBranch;

/// <summary>
/// Edit a branch, move it between regions, or retire it.
/// </summary>
/// <remarks>
/// Moving the head-office flag from one branch to another in two separate
/// requests transiently leaves two branches flagged, and
/// <c>UX_Branch_OneHeadOffice</c> rejects the second one with a 409. That is
/// the correct outcome: the administrator must clear the old head office
/// first, and the message says so.
/// </remarks>
public sealed class UpdateBranchHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateBranchCommand, UpdateBranchResponse>
{
    public async Task<Result<UpdateBranchResponse>> HandleAsync(
        UpdateBranchCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var branch = await db.Branches.SingleOrDefaultAsync(l => l.Id == request.Id, ct);
        if (branch is null)
        {
            return Error.NotFound("Branch", request.Id);
        }

        branch.BranchCode = request.BranchCode;
        branch.BranchName = request.BranchName;
        branch.RegionId = request.RegionId;
        branch.TimeZoneId = request.TimeZoneId;
        branch.IsHeadOffice = request.IsHeadOffice;
        branch.IsActive = request.IsActive;
        branch.ModifiedOnUtc = clock.UtcNow;
        branch.ModifiedBy = currentUser.Username;

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

        return new UpdateBranchResponse(
            branch.Id, branch.BranchCode, branch.IsHeadOffice, branch.IsActive);
    }
}
