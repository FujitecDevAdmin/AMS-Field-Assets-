namespace AMS.Modules.Contracts.Features.SetReminderWindows;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetReminderWindowsRequest(
    int? ContractId,
    IReadOnlyList<SetReminderWindowsRequest.Window> Windows)
{
    /// <summary>One reminder window, as the settings screen sends it.</summary>
    public sealed record Window(int DaysBeforeExpiry, string? Recipients, string? Channel);
}
