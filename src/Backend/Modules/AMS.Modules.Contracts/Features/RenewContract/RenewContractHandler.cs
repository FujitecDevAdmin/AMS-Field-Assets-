using AMS.Modules.Contracts.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.RenewContract;

/// <summary>Extend a contract. Catalogue: Contract Detail.</summary>
/// <remarks>
/// <para>
/// The same row, a new end date, and the renewal counted. Not a new contract:
/// the number is the same, the vendor is the same, and the assets it covers do
/// not want re-linking every year. What changed is readable through
/// <c>FOR SYSTEM_TIME AS OF</c>, which is why the table is versioned.
/// </para>
/// <para>
/// R2-2 is what makes this work with the reminder job: the log's unique key
/// includes the expiry it was measured against, so a renewed contract earns its
/// reminders again for the NEW date rather than being permanently silent
/// because it was reminded about last year's.
/// </para>
/// </remarks>
public sealed class RenewContractHandler(
    ContractsDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<RenewContractCommand, RenewContractResponse>
{
    public async Task<Result<RenewContractResponse>> HandleAsync(
        RenewContractCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contract = await db.Contracts
            .SingleOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);

        if (contract is null)
        {
            return Error.NotFound("Contract", request.Id);
        }

        // Renewing to a date it has already passed is not a renewal. Most often
        // it is somebody typing the current end date again.
        if (request.NewEndDate <= contract.EndDate)
        {
            return Error.Validation(
                "Contract.RenewalNotLater",
                $"The new end date must be after the current one ({contract.EndDate:yyyy-MM-dd}).");
        }

        contract.EndDate = request.NewEndDate;
        contract.RenewalCount++;
        contract.ContractValue = request.ContractValue ?? contract.ContractValue;
        contract.ModifiedOnUtc = clock.UtcNow;
        contract.ModifiedBy = currentUser.Username;

        if (request.Remarks is { Length: > 0 } remarks)
        {
            // Appended rather than replaced. The reason for last year's renewal
            // is still worth having when this year's is recorded.
            contract.Remarks = string.IsNullOrWhiteSpace(contract.Remarks)
                ? remarks
                : $"{contract.Remarks}{Environment.NewLine}{remarks}";

            if (contract.Remarks.Length > 1000)
            {
                contract.Remarks = contract.Remarks[^1000..];
            }
        }

        await db.SaveChangesAsync(ct);

        return new RenewContractResponse(
            contract.Id, contract.EndDate, contract.RenewalCount);
    }
}
