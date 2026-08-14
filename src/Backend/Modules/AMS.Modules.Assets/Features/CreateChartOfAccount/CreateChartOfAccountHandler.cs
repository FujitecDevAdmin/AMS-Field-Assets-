using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.CreateChartOfAccount;

/// <summary>Add a chart-of-account code.</summary>
public sealed class CreateChartOfAccountHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateChartOfAccountCommand, CreateChartOfAccountResponse>
{
    public async Task<Result<CreateChartOfAccountResponse>> HandleAsync(
        CreateChartOfAccountCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = new ChartOfAccount
        {
            CoaCode = request.CoaCode,
            Description = request.Description,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.ChartOfAccounts.Add(account);

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

        return new CreateChartOfAccountResponse(account.Id, account.CoaCode);
    }
}
