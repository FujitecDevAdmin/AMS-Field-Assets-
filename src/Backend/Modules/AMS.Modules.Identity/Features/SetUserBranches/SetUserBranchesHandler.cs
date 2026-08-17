using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.SetUserBranches;

/// <summary>
/// Replace the branches a user sees. Catalogue: Set which branches a user sees.
/// </summary>
/// <remarks>
/// <para>
/// Rows are deleted and re-inserted rather than patched. The primary flag moves
/// between rows, and <c>UX_UserBranch_OnePrimary</c> is a filtered unique index:
/// updating two rows in the wrong order transiently creates two primaries and
/// the statement fails. Replacing the set sidesteps the ordering entirely.
/// </para>
/// <para>
/// Branch ids belong to <c>Organization.Branch</c> and carry no foreign key
/// (01 §2 rule 2), so this handler cannot verify they exist and does not
/// pretend to.
/// </para>
/// </remarks>
public sealed class SetUserBranchesHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<SetUserBranchesCommand, SetUserBranchesResponse>
{
    public async Task<Result<SetUserBranchesResponse>> HandleAsync(
        SetUserBranchesCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        var wanted = request.BranchIds.Distinct().OrderBy(id => id).ToList();

        var current = await db.UserBranches.Where(b => b.UserId == request.UserId).ToListAsync(ct);
        db.UserBranches.RemoveRange(current);

        foreach (var branchId in wanted)
        {
            db.UserBranches.Add(new UserBranch
            {
                UserId = request.UserId,
                BranchId = branchId,
                IsPrimary = branchId == request.PrimaryBranchId,
                GrantedOnUtc = clock.UtcNow,
                GrantedBy = currentUser.Username,
            });
        }

        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = currentUser.Username;

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

        return new SetUserBranchesResponse(request.UserId, wanted, request.PrimaryBranchId);
    }
}
