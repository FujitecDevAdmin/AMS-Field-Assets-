using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.GetEmployee;

/// <summary>
/// One employee, as the directory form edits them.
/// </summary>
public sealed record GetEmployeeQuery(
    int EmployeeId) : IQuery<GetEmployeeResponse>;
