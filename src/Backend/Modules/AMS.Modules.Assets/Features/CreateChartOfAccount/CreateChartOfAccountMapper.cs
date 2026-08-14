namespace AMS.Modules.Assets.Features.CreateChartOfAccount;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class CreateChartOfAccountMapper
{
    public static CreateChartOfAccountCommand ToCommand(CreateChartOfAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CreateChartOfAccountCommand(
            request.CoaCode.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());
    }
}
