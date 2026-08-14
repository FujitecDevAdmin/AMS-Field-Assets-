namespace AMS.Modules.Assets.Features.UpdateChartOfAccount;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateChartOfAccountRequest(
    string CoaCode,
    string? Description,
    bool IsActive);
