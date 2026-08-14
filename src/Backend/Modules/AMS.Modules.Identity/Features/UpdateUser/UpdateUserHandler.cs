using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.UpdateUser;

/// <summary>
/// Edit a user. Catalogue: Create and maintain users.
/// </summary>
/// <remarks>
/// The reference for optimistic concurrency: the caller sends back the ETag
/// they were given, it becomes the original RowVersion, and a mismatch is a
/// 412 rather than a silent overwrite of somebody else's edit.
/// </remarks>
public sealed class UpdateUserHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    public async Task<Result<UpdateUserResponse>> HandleAsync(UpdateUserCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, ct);
        if (user is null)
        {
            return Error.NotFound("User", request.UserId);
        }

        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(request.ETag);
        }
        catch (FormatException)
        {
            // A malformed ETag is the client's fault, not a stale edit.
            return Error.Validation("User.ETagMalformed", "The record version supplied is not valid.");
        }

        // Telling EF what we THOUGHT the row looked like is what makes the
        // UPDATE's WHERE clause carry the version.
        db.Entry(user).Property(u => u.RowVersion).OriginalValue = expected;

        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.EmployeeId = request.EmployeeId;
        user.IsActive = request.IsActive;
        user.HasAllBranches = request.HasAllBranches;
        user.ModifiedOnUtc = clock.UtcNow;
        user.ModifiedBy = currentUser.Username;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Concurrency(
                "User.Stale",
                "This record changed while you were editing it. Reload and try again.");
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

        return new UpdateUserResponse(user.Id, user.DisplayName, Convert.ToBase64String(user.RowVersion));
    }
}
