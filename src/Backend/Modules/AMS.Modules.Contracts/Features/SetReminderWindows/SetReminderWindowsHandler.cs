using AMS.Modules.Contracts.Domain;
using AMS.Modules.Contracts.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.SetReminderWindows;

/// <summary>
/// Set when expiry reminders go out. Catalogue: reminder settings.
/// </summary>
/// <remarks>
/// <para>
/// These were 60/30/15/7 days compiled into a job, and one AMC needing ninety
/// days' notice meant a release. They are rows now: a null ContractId is the
/// organisation default, a non-null one overrides it for that contract.
/// </para>
/// <para>
/// The whole set at once, and an override REPLACES the default for that
/// contract rather than adding to it. Merging the two would mean a contract
/// that wants only a ninety-day warning still gets the seven-day one, and there
/// would be no way to ask for less.
/// </para>
/// </remarks>
public sealed class SetReminderWindowsHandler(
    ContractsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetReminderWindowsCommand, SetReminderWindowsResponse>
{
    public async Task<Result<SetReminderWindowsResponse>> HandleAsync(
        SetReminderWindowsCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ContractId is { } contractId
            && !await db.Contracts.AnyAsync(c => c.Id == contractId && !c.IsDeleted, ct))
        {
            return Error.NotFound("Contract", contractId);
        }

        var invalid = Validate(request.Windows);
        if (invalid is not null)
        {
            return invalid;
        }

        var existing = await db.ContractReminderSettings
            .Where(s => s.ContractId == request.ContractId)
            .ToListAsync(ct);

        db.ContractReminderSettings.RemoveRange(existing);

        // Saved before the new rows go in. Both unique indexes are on
        // (contract, days), and a delete and an insert of the same window in
        // one batch collide - EF has no reason to order them.
        await db.SaveChangesAsync(ct);

        var now = clock.UtcNow;

        foreach (var window in request.Windows)
        {
            db.ContractReminderSettings.Add(new ContractReminderSetting
            {
                ContractId = request.ContractId,
                DaysBeforeExpiry = window.DaysBeforeExpiry,
                Recipients = window.Recipients,
                Channel = window.Channel,
                IsActive = true,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
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

        return new SetReminderWindowsResponse(
            request.ContractId, request.Windows.Count, request.ContractId is null);
    }

    private static Error? Validate(IReadOnlyList<SetReminderWindowsCommand.Window> windows)
    {
        var seen = new HashSet<int>();

        foreach (var window in windows)
        {
            if (window.DaysBeforeExpiry is < 1 or > 365)
            {
                return Error.Validation(
                    "ContractReminder.Days",
                    "A reminder window is 1 to 365 days before expiry.");
            }

            if (!seen.Add(window.DaysBeforeExpiry))
            {
                return Error.Validation(
                    "ContractReminder.DuplicateWindow",
                    $"{window.DaysBeforeExpiry} days appears twice.");
            }

            if (!ReminderChannel.Allowed.Contains(window.Channel, StringComparer.Ordinal))
            {
                return Error.Validation(
                    "ContractReminder.UnknownChannel",
                    $"Channel must be one of {string.Join(", ", ReminderChannel.Allowed)}.");
            }
        }

        return null;
    }
}
