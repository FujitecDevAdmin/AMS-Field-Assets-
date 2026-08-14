using AMS.Modules.Contracts.Features.CreateContract;
using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Contracts.Reminders;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.UpdateContract;

/// <summary>Edit a contract or retire it. Catalogue: Contract Detail.</summary>
/// <remarks>
/// <para>
/// The contract NUMBER is not editable. It is how the contract is quoted — on
/// an invoice, in an e-mail to the vendor, on a purchase order — and changing
/// it would break every reference outside this system, none of which we can
/// see.
/// </para>
/// <para>
/// An empty licence key means "leave it alone", not "clear it", for the same
/// reason the SMTP password does: the screen cannot show the stored one, so it
/// cannot send it back, and treating the blank field as a deletion would wipe
/// the key every time somebody corrected a date.
/// </para>
/// <para>
/// Retiring is a flag. The table is system-versioned and a contract that
/// covered an asset last year is what explains why a repair was free.
/// </para>
/// </remarks>
public sealed class UpdateContractHandler(
    ContractsDbContext db,
    IVendorDirectory vendors,
    LicenceKeyProtector protector,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateContractCommand, UpdateContractResponse>
{
    public async Task<Result<UpdateContractResponse>> HandleAsync(
        UpdateContractCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contract = await db.Contracts.SingleOrDefaultAsync(c => c.Id == request.Id, ct);
        if (contract is null)
        {
            return Error.NotFound("Contract", request.Id);
        }

        var invalid = await CreateContractHandler.ValidateAsync(
            vendors, contract.ContractType, request.StartDate, request.EndDate,
            request.VendorId, ct);

        if (invalid is not null)
        {
            return invalid;
        }

        contract.ContractName = request.ContractName;
        contract.VendorId = request.VendorId;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.ContractValue = request.ContractValue;
        contract.LicensedSeats = request.LicensedSeats;
        contract.AutoRenew = request.AutoRenew;
        contract.Remarks = request.Remarks;
        contract.IsDeleted = request.IsDeleted;
        contract.ModifiedOnUtc = clock.UtcNow;
        contract.ModifiedBy = currentUser.Username;

        if (!string.IsNullOrEmpty(request.LicenceKey))
        {
            contract.LicenseKeyEncrypted = protector.Protect(request.LicenceKey);
        }

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

        return new UpdateContractResponse(
            contract.Id, contract.ContractNumber, contract.IsDeleted);
    }
}
