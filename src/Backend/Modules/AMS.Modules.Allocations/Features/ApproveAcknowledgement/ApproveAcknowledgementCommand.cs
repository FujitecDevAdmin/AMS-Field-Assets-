using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.ApproveAcknowledgement;

/// <summary>
/// The manager countersigns. Catalogue: Approve the acknowledgement.
/// </summary>
public sealed record ApproveAcknowledgementCommand(
    int AllocationId) : ICommand<ApproveAcknowledgementResponse>;
