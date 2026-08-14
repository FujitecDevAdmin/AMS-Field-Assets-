namespace AMS.Modules.Assets.Features.UpdateChartOfAccount;

/// <summary>
/// The updated code.
/// </summary>
/// <param name="Id">The code edited.</param>
/// <param name="CoaCode">Unique.</param>
/// <param name="IsActive">Retiring hides it from pickers; finance records already pointing here keep it.</param>
public sealed record UpdateChartOfAccountResponse(
    int Id,
    string CoaCode,
    bool IsActive);
