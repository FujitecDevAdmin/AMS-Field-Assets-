using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Contracts.Reminders;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.GetContract;

/// <summary>One contract with what it covers. Catalogue: Contract Detail.</summary>
/// <remarks>
/// The reminder windows come back RESOLVED — the contract's own where it has
/// them, the organisation default where it does not, each flagged. The screen
/// has to be able to tell them apart, because editing an inherited window
/// creates an override and somebody should know that before they do it.
/// </remarks>
public sealed class GetContractHandler(
    ContractsDbContext db,
    IVendorDirectory vendors,
    IClock clock)
    : IRequestHandler<GetContractQuery, GetContractResponse>
{
    public async Task<Result<GetContractResponse>> HandleAsync(
        GetContractQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contract = await db.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == request.Id && !c.IsDeleted, ct);

        if (contract is null)
        {
            return Error.NotFound("Contract", request.Id);
        }

        var vendor = contract.VendorId is { } vendorId
            ? await vendors.FindAsync(vendorId, ct)
            : null;

        var assetIds = await db.ContractAssets
            .AsNoTracking()
            .Where(a => a.ContractId == contract.Id)
            .OrderBy(a => a.AssetId)
            .Select(a => a.AssetId)
            .ToListAsync(ct);

        var documents = await db.ContractDocuments
            .AsNoTracking()
            .Where(d => d.ContractId == contract.Id)
            .OrderByDescending(d => d.UploadedOnUtc)
            .Select(d => new GetContractResponse.Document(
                d.Id, d.FileName, d.ContentType, d.SizeBytes, d.UploadedOnUtc))
            .ToListAsync(ct);

        var windows = await ReminderWindows.ResolveAsync(db, contract.Id, ct);

        var sent = await db.ContractReminderLogs
            .AsNoTracking()
            .Where(l => l.ContractId == contract.Id)
            .OrderByDescending(l => l.SentOnDate)
            .ThenByDescending(l => l.Id)
            .Select(l => new GetContractResponse.SentReminder(
                l.DaysBeforeExpiry, l.ExpiryDateSnapshot, l.SentOnDate, l.SentTo, l.Outcome))
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(clock.UtcNow);

        return new GetContractResponse(
            contract.Id,
            contract.ContractNumber,
            contract.ContractName,
            contract.ContractType,
            contract.VendorId,
            vendor?.VendorName,
            contract.StartDate,
            contract.EndDate,
            contract.EndDate.DayNumber - today.DayNumber,
            contract.ContractValue,
            contract.LicensedSeats,
            // Whether a key is stored, never the key. docs/03 §8: encrypted
            // columns are excluded from any projection that feeds a screen.
            contract.LicenseKeyEncrypted is { Length: > 0 },
            contract.AutoRenew,
            contract.RenewalCount,
            contract.Remarks,
            assetIds,
            documents,
            [.. windows.Select(w => new GetContractResponse.ReminderWindow(
                w.DaysBeforeExpiry, w.Recipients, w.Channel, w.IsContractSpecific))],
            sent);
    }
}
