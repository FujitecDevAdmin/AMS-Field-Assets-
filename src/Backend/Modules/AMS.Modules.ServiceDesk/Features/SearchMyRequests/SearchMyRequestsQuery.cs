using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchMyRequests;

/// <summary>
/// What I have asked for and where it has got to. Catalogue: My Requests.
/// </summary>
public sealed record SearchMyRequestsQuery(
    int EmployeeId,
    bool OpenOnly,
    int Skip,
    int Take) : IQuery<SearchMyRequestsResponse>;
