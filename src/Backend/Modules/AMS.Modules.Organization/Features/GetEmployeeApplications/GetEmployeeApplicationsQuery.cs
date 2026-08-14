using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.GetEmployeeApplications;

/// <summary>
/// What one employee has been granted. Catalogue screen: Applications and Access.
/// </summary>
public sealed record GetEmployeeApplicationsQuery(
    int EmployeeId,
    bool IncludeRevoked) : IQuery<GetEmployeeApplicationsResponse>;
