using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateChartOfAccount;

/// <summary>Edit a chart-of-account code's description, or retire it.</summary>
public sealed class UpdateChartOfAccountHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateChartOfAccountCommand, UpdateChartOfAccountResponse>
{
    public async Task<Result<UpdateChartOfAccountResponse>> HandleAsync(
        UpdateChartOfAccountCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await db.ChartOfAccounts.SingleOrDefaultAsync(c => c.Id == request.Id, ct);
        if (account is null)
        {
            return Error.NotFound("ChartOfAccount", request.Id);
        }

        account.CoaCode = request.CoaCode;
        account.Description = request.Description;
        account.IsActive = request.IsActive;
        account.ModifiedOnUtc = clock.UtcNow;
        account.ModifiedBy = currentUser.Username;

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

        return new UpdateChartOfAccountResponse(account.Id, account.CoaCode, account.IsActive);
    }
}
