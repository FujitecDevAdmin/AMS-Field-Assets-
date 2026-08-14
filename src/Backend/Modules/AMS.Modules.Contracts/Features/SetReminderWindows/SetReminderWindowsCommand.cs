using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.SetReminderWindows;

/// <summary>
/// Set when expiry reminders go out — for everything, or for one contract. Catalogue: reminder settings.
/// </summary>
public sealed record SetReminderWindowsCommand(
    int? ContractId,
    IReadOnlyList<SetReminderWindowsCommand.Window> Windows) : ICommand<SetReminderWindowsResponse>
{
    /// <summary>One reminder window.</summary>
    /// <param name="DaysBeforeExpiry">
    /// How long before the end date. 1 to 365, as
    /// CK_ContractReminderSetting_Days allows.
    /// </param>
    /// <param name="Recipients">
    /// Who to write to, semicolon separated. Blank means the vendor contact —
    /// which is the useful default, because the person who can do something
    /// about an expiring AMC usually works for the vendor.
    /// </param>
    /// <param name="Channel">Email, InApp or Both.</param>
    public sealed record Window(int DaysBeforeExpiry, string? Recipients, string Channel);
}
