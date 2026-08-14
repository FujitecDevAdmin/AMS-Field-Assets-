using AMS.Modules.Contracts.Domain;

namespace AMS.Modules.Contracts.Features.SetReminderWindows;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetReminderWindowsMapper
{
    public static SetReminderWindowsCommand ToCommand(SetReminderWindowsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetReminderWindowsCommand(
            request.ContractId,
            [.. request.Windows.Select(w => new SetReminderWindowsCommand.Window(
                w.DaysBeforeExpiry,
                string.IsNullOrWhiteSpace(w.Recipients) ? null : w.Recipients.Trim(),
                string.IsNullOrWhiteSpace(w.Channel) ? ReminderChannel.Email : w.Channel.Trim()))]);
    }
}
