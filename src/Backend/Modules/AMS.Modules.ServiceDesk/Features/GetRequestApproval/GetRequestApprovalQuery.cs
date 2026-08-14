using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.GetRequestApproval;

/// <summary>
/// The approval run on one request, with every level and every decision. Catalogue: the approval panel on Request Detail.
/// </summary>
public sealed record GetRequestApprovalQuery(
    int Id) : IQuery<GetRequestApprovalResponse>;
