using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.GetServiceRequest;

/// <summary>
/// One ticket with its conversation, its files and its clock. Catalogue: Request Detail.
/// </summary>
public sealed record GetServiceRequestQuery(
    int Id,
    bool IncludeInternal) : IQuery<GetServiceRequestResponse>;
