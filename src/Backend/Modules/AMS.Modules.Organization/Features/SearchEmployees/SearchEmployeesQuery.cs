using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchEmployees;

/// <summary>
/// The employee directory, filtered and paged.
/// </summary>
public sealed record SearchEmployeesQuery(
    string? Search,
    int? DepartmentId,
    int? LocationId,
    bool? IsActive,
    int Skip,
    int Take) : IQuery<SearchEmployeesResponse>;
