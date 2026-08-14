using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SubmitForApproval;

/// <summary>
/// Send a new service request for approval. Catalogue: Submit for Approval on Request Detail.
/// </summary>
public sealed record SubmitForApprovalCommand(
    int Id,
    int? ApprovalWorkflowId) : ICommand<SubmitForApprovalResponse>;
