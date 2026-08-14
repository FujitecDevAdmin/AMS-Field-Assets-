using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.SearchDepartments;

/// <summary>
/// The department list. Catalogue screen: Departments.
/// </summary>
public sealed record SearchDepartmentsQuery(
    bool? IsActive,
    string? Search) : IQuery<SearchDepartmentsResponse>;
