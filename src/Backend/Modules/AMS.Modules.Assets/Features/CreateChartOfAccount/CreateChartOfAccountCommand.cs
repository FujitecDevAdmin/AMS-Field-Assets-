using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.CreateChartOfAccount;

/// <summary>
/// Add a chart-of-account code.
/// </summary>
public sealed record CreateChartOfAccountCommand(
    string CoaCode,
    string? Description) : ICommand<CreateChartOfAccountResponse>;
