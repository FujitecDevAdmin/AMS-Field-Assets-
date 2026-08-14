namespace AMS.Modules.Assets.Features.UpdateChartOfAccount;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class UpdateChartOfAccountMapper
{
    public static UpdateChartOfAccountCommand ToCommand(UpdateChartOfAccountRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new UpdateChartOfAccountCommand(
            id,
            request.CoaCode.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            request.IsActive);
    }
}
