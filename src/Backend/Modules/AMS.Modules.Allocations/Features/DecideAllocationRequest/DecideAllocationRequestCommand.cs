using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.DecideAllocationRequest;

/// <summary>
/// Approve or reject a request. Catalogue: Approve or reject a request — with a decision remark that stays on the record.
/// </summary>
public sealed record DecideAllocationRequestCommand(
    int Id,
    bool Approved,
    string? DecisionRemarks) : ICommand<DecideAllocationRequestResponse>;
