using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.GetMyApplicationAccess;

/// <summary>
/// What the signed-in employee has been granted. Catalogue: See my application access - a read-only view.
/// </summary>
public sealed record GetMyApplicationAccessQuery(
    int? EmployeeId) : IQuery<GetMyApplicationAccessResponse>;
