using AMS.Modules.Contracts.Domain;
using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Contracts.Reminders;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.CreateContract;

/// <summary>Record a contract. Catalogue: Contracts.</summary>
/// <remarks>
/// The covered assets arrive with it. A contract saved with nothing covered and
/// linked a minute later is a contract that briefly covered nothing — and if
/// the second call never happens, it stays that way looking correct.
/// </remarks>
public sealed class CreateContractHandler(
    ContractsDbContext db,
    IVendorDirectory vendors,
    LicenceKeyProtector protector,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateContractCommand, CreateContractResponse>
{
    public async Task<Result<CreateContractResponse>> HandleAsync(
        CreateContractCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invalid = await ValidateAsync(
            vendors, request.ContractType, request.StartDate, request.EndDate,
            request.VendorId, ct);

        if (invalid is not null)
        {
            return invalid;
        }

        var now = clock.UtcNow;

        var contract = new Contract
        {
            ContractNumber = request.ContractNumber,
            ContractName = request.ContractName,
            ContractType = request.ContractType,
            VendorId = request.VendorId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ContractValue = request.ContractValue,
            LicensedSeats = request.LicensedSeats,
            LicenseKeyEncrypted = string.IsNullOrEmpty(request.LicenceKey)
                ? null
                : protector.Protect(request.LicenceKey),
            AutoRenew = request.AutoRenew,
            RenewalCount = 0,
            Remarks = request.Remarks,
            IsDeleted = false,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.Contracts.Add(contract);

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

        foreach (var assetId in request.AssetIds.Distinct())
        {
            db.ContractAssets.Add(new ContractAsset
            {
                ContractId = contract.Id,
                AssetId = assetId,
                LinkedOnUtc = now,
                LinkedByUserId = currentUser.Id,
            });
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

        return new CreateContractResponse(
            contract.Id, contract.ContractNumber, request.AssetIds.Distinct().Count());
    }

    /// <summary>Rules the create and update slices share.</summary>
    internal static async Task<Error?> ValidateAsync(
        IVendorDirectory vendors,
        string contractType,
        DateOnly startDate,
        DateOnly endDate,
        int? vendorId,
        CancellationToken ct)
    {
        if (!ContractType.Allowed.Contains(contractType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Contract.UnknownType",
                $"Contract type must be one of {string.Join(", ", ContractType.Allowed)}.");
        }

        // CK_Contract_Window says the same thing. Saying it here means an
        // administrator gets a sentence rather than a 500 naming a constraint.
        if (endDate < startDate)
        {
            return Error.Validation(
                "Contract.Window",
                "A contract cannot end before it starts.");
        }

        return vendorId is { } id && !await vendors.IsActiveAsync(id, ct)
            ? Error.NotFound("Vendor", id)
            : null;
    }
}
