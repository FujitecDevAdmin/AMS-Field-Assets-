using AMS.Modules.Organization.Domain;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Features.CreateBranch;

/// <summary>
/// Open a branch. Catalogue: Branches and branches, Put a branch in a region,
/// Branch time zone.
/// </summary>
/// <remarks>
/// Two database rules do the work here, and neither is re-implemented in code:
/// <c>UX_Branch_Code</c> keeps codes unique, and
/// <c>UX_Branch_OneHeadOffice</c> is a filtered unique index that makes a
/// second head office impossible rather than merely unlikely.
/// </remarks>
public sealed class CreateBranchHandler(
    OrganizationDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<CreateBranchCommand, CreateBranchResponse>
{
    public async Task<Result<CreateBranchResponse>> HandleAsync(
        CreateBranchCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var branch = new Branch
        {
            BranchCode = request.BranchCode,
            BranchName = request.BranchName,
            RegionId = request.RegionId,
            TimeZoneId = request.TimeZoneId,
            IsHeadOffice = request.IsHeadOffice,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.Branches.Add(branch);

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

        return new CreateBranchResponse(
            branch.Id, branch.BranchCode, branch.BranchName, branch.IsHeadOffice);
    }
}
