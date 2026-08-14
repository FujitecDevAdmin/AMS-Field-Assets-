using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateChartOfAccount;

/// <summary>
/// Edit a chart-of-account code's description, or retire it.
/// </summary>
public sealed record UpdateChartOfAccountCommand(
    int Id,
    string CoaCode,
    string? Description,
    bool IsActive) : ICommand<UpdateChartOfAccountResponse>;
