using AMS.Modules.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Reminders;

/// <summary>One reminder window that applies to a contract.</summary>
/// <param name="DaysBeforeExpiry">How long before the end date it goes out.</param>
/// <param name="Recipients">Who to. Blank means the vendor contact.</param>
/// <param name="Channel">Email, InApp or Both.</param>
/// <param name="IsContractSpecific">
/// Whether this contract has its own setting, or is inheriting the
/// organisation default.
/// </param>
public sealed record ReminderWindow(
    int DaysBeforeExpiry,
    string? Recipients,
    string Channel,
    bool IsContractSpecific);

/// <summary>
/// Which reminder windows actually apply to a contract.
/// </summary>
/// <remarks>
/// <para>
/// A null <c>ContractId</c> is the organisation default; a non-null one
/// overrides it. The override REPLACES rather than adds to: merging would mean
/// a contract that wants only a ninety-day warning still gets the seven-day
/// one, and there would be no way to ask for less.
/// </para>
/// <para>
/// It lives here, on its own, because two things need it — the detail screen
/// that shows what will happen and the worker that makes it happen. Two copies
/// of "an override replaces the default" would be two chances for the screen to
/// be a lie.
/// </para>
/// </remarks>
public static class ReminderWindows
{
    /// <summary>The windows in force for one contract, soonest to expiry last.</summary>
    public static async Task<IReadOnlyList<ReminderWindow>> ResolveAsync(
        ContractsDbContext db,
        int contractId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);

        var own = await db.ContractReminderSettings
            .AsNoTracking()
            .Where(s => s.ContractId == contractId && s.IsActive)
            .OrderByDescending(s => s.DaysBeforeExpiry)
            .Select(s => new ReminderWindow(
                s.DaysBeforeExpiry, s.Recipients, s.Channel, true))
            .ToListAsync(ct);

        if (own.Count > 0)
        {
            return own;
        }

        return await db.ContractReminderSettings
            .AsNoTracking()
            .Where(s => s.ContractId == null && s.IsActive)
            .OrderByDescending(s => s.DaysBeforeExpiry)
            .Select(s => new ReminderWindow(
                s.DaysBeforeExpiry, s.Recipients, s.Channel, false))
            .ToListAsync(ct);
    }

    /// <summary>
    /// The windows in force for many contracts at once.
    /// </summary>
    /// <remarks>
    /// The worker's version. Asking per contract would be two queries for every
    /// contract in the system, every day, to answer a question whose answer is
    /// the same for almost all of them.
    /// </remarks>
    public static async Task<IReadOnlyDictionary<int, IReadOnlyList<ReminderWindow>>>
        ResolveManyAsync(
            ContractsDbContext db,
            IReadOnlyList<int> contractIds,
            CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(contractIds);

        var settings = await db.ContractReminderSettings
            .AsNoTracking()
            .Where(s => s.IsActive
                && (s.ContractId == null || contractIds.Contains(s.ContractId.Value)))
            .ToListAsync(ct);

        var defaults = settings
            .Where(s => s.ContractId is null)
            .OrderByDescending(s => s.DaysBeforeExpiry)
            .Select(s => new ReminderWindow(s.DaysBeforeExpiry, s.Recipients, s.Channel, false))
            .ToList();

        var overrides = settings
            .Where(s => s.ContractId is not null)
            .GroupBy(s => s.ContractId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ReminderWindow>)[.. g
                    .OrderByDescending(s => s.DaysBeforeExpiry)
                    .Select(s => new ReminderWindow(
                        s.DaysBeforeExpiry, s.Recipients, s.Channel, true))]);

        return contractIds.ToDictionary(
            id => id,
            id => overrides.TryGetValue(id, out var own) ? own : defaults);
    }
}
