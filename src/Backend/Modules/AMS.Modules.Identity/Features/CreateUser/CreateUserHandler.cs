using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.CreateUser;

/// <summary>
/// The reference command handler. Everything the other fifteen modules copy is
/// visible here (docs/02 §4).
/// </summary>
public sealed class CreateUserHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<Result<CreateUserResponse>> HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = clock.UtcNow;                       // never DateTime.UtcNow — 02 §4

        var user = new User
        {
            Username = request.Username,
            DisplayName = request.DisplayName,
            PasswordHash = request.PasswordHash,
            Email = request.Email,
            EmployeeId = request.EmployeeId,
            HasAllBranches = request.HasAllBranches,
            MustChangePassword = true,                // a new account sets its own password
            IsActive = true,
            IsLocked = false,
            FailedLoginAttempts = 0,
            MfaEnabled = false,
            MfaEnrollmentRequired = false,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.Users.Add(user);

        foreach (var branchId in request.BranchIds.Distinct())
        {
            db.UserBranches.Add(new UserBranch
            {
                UserId = user.Id,                     // filled in by the fixup on save
                BranchId = branchId,
                IsPrimary = branchId == request.PrimaryBranchId,
                GrantedOnUtc = now,
                GrantedBy = currentUser.Username,
            });
        }

        try
        {
            // One SaveChanges for this module's work. Cross-module effects
            // would go through a PublicApi write contract inside the same
            // transaction (01 rule 4a) — this slice has none.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            // The database is the law: UX_User_Username and UX_UserBranch_OnePrimary
            // decide, not a read-then-write check in this method (03 §1 rule 6).
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return CreateUserMapper.ToResponse(user);
    }
}
