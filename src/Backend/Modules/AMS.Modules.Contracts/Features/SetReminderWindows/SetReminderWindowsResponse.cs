namespace AMS.Modules.Contracts.Features.SetReminderWindows;

/// <summary>
/// The windows as they now stand.
/// </summary>
/// <param name="ContractId">The contract these apply to, or null for the organisation default.</param>
/// <param name="WindowCount">How many reminders will go out.</param>
/// <param name="IsDefault">True when this replaced the organisation-wide setting.</param>
public sealed record SetReminderWindowsResponse(
    int? ContractId,
    int WindowCount,
    bool IsDefault);
