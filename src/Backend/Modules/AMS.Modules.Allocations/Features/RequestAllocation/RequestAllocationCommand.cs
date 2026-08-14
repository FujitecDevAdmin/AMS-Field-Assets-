using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.RequestAllocation;

/// <summary>
/// Ask for an asset to be allocated to an employee. Catalogue: Request an asset for an employee.
/// </summary>
public sealed record RequestAllocationCommand(
    int AssetId,
    int EmployeeId,
    int? LocationId,
    string? Remarks) : ICommand<RequestAllocationResponse>;
