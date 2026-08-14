using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.CancelApproval;

/// <summary>
/// Call off an approval run, with a reason. Catalogue: the approval panel on Request Detail.
/// </summary>
public sealed record CancelApprovalCommand(
    int Id,
    string Reason) : ICommand<CancelApprovalResponse>;
