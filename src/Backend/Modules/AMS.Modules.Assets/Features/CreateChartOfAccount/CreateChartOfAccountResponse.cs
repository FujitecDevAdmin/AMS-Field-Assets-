namespace AMS.Modules.Assets.Features.CreateChartOfAccount;

/// <summary>
/// The new code.
/// </summary>
/// <param name="Id">The new code.</param>
/// <param name="CoaCode">Unique. Stored once so 7,000 assets cannot hold 7,000 copies of one description.</param>
public sealed record CreateChartOfAccountResponse(
    int Id,
    string CoaCode);
