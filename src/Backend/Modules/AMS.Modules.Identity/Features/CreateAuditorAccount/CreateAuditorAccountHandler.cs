using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Features.CreateAuditorAccount;

public sealed class CreateAuditorAccountHandler(
    IdentityDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateAuditorAccountCommand, CreateAuditorAccountResponse>
{
    public async Task<Result<CreateAuditorAccountResponse>> HandleAsync(
        CreateAuditorAccountCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = clock.UtcNow;
        var auditorRole = await db.Roles.SingleOrDefaultAsync(role => role.RoleName == "Auditor", ct);
        if (auditorRole is null)
        {
            auditorRole = new Role
            {
                RoleName = "Auditor",
                Description = "Performs physical field-asset verification for assigned locations.",
                IsSystemRole = true,
                IsActive = true,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
            };
            db.Roles.Add(auditorRole);
        }

        if (!await db.Capabilities.AnyAsync(capability => capability.Name == "verification.run", ct))
        {
            return Error.NotFound("Capability", "verification.run");
        }

        var user = new User
        {
            Username = request.Username,
            DisplayName = request.DisplayName,
            PasswordHash = request.PasswordHash,
            Email = request.Email,
            EmployeeId = request.EmployeeId,
            HasAllBranches = request.HasAllBranches,
            MustChangePassword = true,
            IsActive = true,
            IsLocked = false,
            FailedLoginAttempts = 0,
            MfaEnabled = false,
            MfaEnrollmentRequired = request.RequireMfa,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            if (!await db.RoleCapabilities.AnyAsync(
                grant => grant.RoleId == auditorRole.Id && grant.CapabilityName == "verification.run", ct))
            {
                db.RoleCapabilities.Add(new RoleCapability
                {
                    RoleId = auditorRole.Id,
                    CapabilityName = "verification.run",
                    GrantedOnUtc = now,
                    GrantedBy = currentUser.Username,
                });
            }

            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = auditorRole.Id,
                GrantedOnUtc = now,
                GrantedBy = currentUser.Username,
            });

            foreach (var branchId in request.BranchIds.Distinct())
            {
                db.UserBranches.Add(new UserBranch
                {
                    UserId = user.Id,
                    BranchId = branchId,
                    IsPrimary = branchId == request.PrimaryBranchId,
                    GrantedOnUtc = now,
                    GrantedBy = currentUser.Username,
                });
            }

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

        return new CreateAuditorAccountResponse(
            user.Id,
            user.Username,
            user.DisplayName,
            user.MustChangePassword,
            user.MfaEnrollmentRequired);
    }
}
